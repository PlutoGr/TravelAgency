#!/usr/bin/env python3
"""После закрытия задачи ставит чип PR в Development (closedBy).

Что делает за один запуск:
  1. Если у задачи уже есть closedByPullRequestsReferences (включая смерженные PR),
     выходит без изменений.
  2. Среди смерженных PR того же репозитория выбирает один лучший:
       номер задачи отдельным сегментом имени ветки (между началом, концом
       или разделителями /, -, _) и/или «#NN» в заголовке или тексте.
       Ключевое слово closes не требуется.
       Dependabot (автор или ветка dependabot/) не кандидат.
  3. Вызывает GraphQL addCloseIssueReferences.
  4. Повторный запуск ничего не меняет: связь уже есть, либо мутация
     отвечает «уже связано», и это считается успехом.

Нет подходящего PR: сообщение в лог и код 0. Ошибка API при найденном PR: код 1.
Старые закрытые задачи этим скриптом сами не обходятся: его вызывает только
workflow на событии закрытия.

Токен: GH_TOKEN (в Actions это GITHUB_TOKEN). Projects API и BOARD_TOKEN не нужны.
Значение токена не печатается.
"""

import argparse
import json
import os
import re
import subprocess
import sys

GH = os.environ.get("GH_BIN", "gh")
MAX_PR_PAGES = 20
PAGE_SIZE = 50

ISSUE_QUERY = """
query($owner: String!, $name: String!, $number: Int!) {
  repository(owner: $owner, name: $name) {
    issue(number: $number) {
      id
      number
      state
      closedByPullRequestsReferences(first: 20, includeClosedPrs: true) {
        pageInfo { hasNextPage }
        nodes { number }
      }
    }
  }
}
"""

PRS_QUERY_FIRST = """
query($owner: String!, $name: String!) {
  repository(owner: $owner, name: $name) {
    pullRequests(states: MERGED, first: %d, orderBy: {field: UPDATED_AT, direction: DESC}) {
      pageInfo { hasNextPage endCursor }
      nodes { id number title body mergedAt headRefName author { login } }
    }
  }
}
""" % PAGE_SIZE

PRS_QUERY_NEXT = """
query($owner: String!, $name: String!, $after: String!) {
  repository(owner: $owner, name: $name) {
    pullRequests(states: MERGED, first: %d, after: $after, orderBy: {field: UPDATED_AT, direction: DESC}) {
      pageInfo { hasNextPage endCursor }
      nodes { id number title body mergedAt headRefName author { login } }
    }
  }
}
""" % PAGE_SIZE

LINK_MUTATION = """
mutation($issueId: ID!, $prId: ID!) {
  addCloseIssueReferences(input: {issueId: $issueId, pullRequestIds: [$prId]}) {
    issue {
      number
      closedByPullRequestsReferences(first: 20, includeClosedPrs: true) {
        nodes { number }
      }
    }
  }
}
"""


class GraphqlError(RuntimeError):
    def __init__(self, message, payload=None):
        super().__init__(message)
        self.payload = payload or {}


def log(message):
    print(message, flush=True)


def branch_has_issue(branch, number):
    """Номер задачи как отдельный сегмент ветки, не часть более длинного числа и не кусок суффикса вроде 3f52."""
    if not branch or number is None:
        return False
    return re.search(rf"(?:^|[/_-]){int(number)}(?:$|[/_-])", branch) is not None


def text_has_issue_ref(text, number):
    """«#NN» в заголовке или тексте. Более длинный номер (#120 для задачи 12) не считается."""
    if not text or number is None:
        return False
    return re.search(rf"(?<!\d)#{int(number)}(?!\d)", text) is not None


def is_dependabot(author, head):
    login = (author or "").lower()
    ref = (head or "").lower()
    return "dependabot" in login or ref.startswith("dependabot/")


def candidate_rank(pr, number):
    """Кортеж (очки, mergedAt, номер PR) или None, если PR не кандидат.

    Очки: сегмент ветки +4, «#NN» в заголовке +2, «#NN» в тексте +1.
    """
    if not pr or not pr.get("merged_at"):
        return None
    if is_dependabot(pr.get("author"), pr.get("head")):
        return None
    score = 0
    if branch_has_issue(pr.get("head"), number):
        score += 4
    if text_has_issue_ref(pr.get("title"), number):
        score += 2
    if text_has_issue_ref(pr.get("body"), number):
        score += 1
    if score == 0:
        return None
    return (score, pr.get("merged_at") or "", int(pr.get("number") or 0))


def pick_best(prs, number):
    """Лучший смерженный PR: больше очков, затем позже смержен, затем больший номер."""
    best = None
    best_rank = None
    for pr in prs or []:
        rank = candidate_rank(pr, number)
        if rank is None:
            continue
        if best_rank is None or rank > best_rank:
            best = pr
            best_rank = rank
    return best


def graphql(query, variables):
    cmd = [GH, "api", "graphql", "-f", f"query={query}"]
    for key, value in variables.items():
        if value is None:
            continue
        if isinstance(value, bool):
            cmd.extend(["-F", f"{key}={'true' if value else 'false'}"])
        elif isinstance(value, int):
            cmd.extend(["-F", f"{key}={value}"])
        else:
            cmd.extend(["-f", f"{key}={value}"])
    proc = subprocess.run(cmd, capture_output=True, text=True)
    stdout = (proc.stdout or "").strip()
    try:
        payload = json.loads(stdout) if stdout else {}
    except json.JSONDecodeError as exc:
        detail = (proc.stderr or "").strip() or stdout or str(exc)
        raise GraphqlError(f"Ответ GraphQL не разобран: {detail}") from exc
    errors = payload.get("errors") or []
    if proc.returncode != 0 or errors:
        messages = [err.get("message") or str(err) for err in errors]
        if not messages and (proc.stderr or "").strip():
            messages.append(proc.stderr.strip())
        raise GraphqlError("; ".join(messages) or f"gh api graphql: код {proc.returncode}", payload)
    return payload


def split_repo(repo):
    owner, _, name = (repo or "").partition("/")
    if not owner or not name:
        raise RuntimeError("Не задан репозиторий OWNER/NAME (GITHUB_REPOSITORY).")
    return owner, name


def load_issue(owner, name, number):
    payload = graphql(ISSUE_QUERY, {"owner": owner, "name": name, "number": number})
    return ((payload.get("data") or {}).get("repository") or {}).get("issue")


def load_merged_prs(owner, name):
    prs = []
    payload = graphql(PRS_QUERY_FIRST, {"owner": owner, "name": name})
    page = 1
    while True:
        block = ((payload.get("data") or {}).get("repository") or {}).get("pullRequests") or {}
        for node in block.get("nodes") or []:
            prs.append({
                "id": node.get("id"),
                "number": node.get("number"),
                "title": node.get("title") or "",
                "body": node.get("body") or "",
                "head": node.get("headRefName") or "",
                "author": ((node.get("author") or {}).get("login") or ""),
                "merged_at": node.get("mergedAt") or "",
            })
        info = block.get("pageInfo") or {}
        if not info.get("hasNextPage"):
            break
        if page >= MAX_PR_PAGES:
            log(f"Просмотрены первые {page * PAGE_SIZE} смерженных PR, дальше обрезка.")
            break
        payload = graphql(PRS_QUERY_NEXT, {"owner": owner, "name": name, "after": info.get("endCursor")})
        page += 1
    return prs


def closed_by_numbers(issue):
    refs = (issue or {}).get("closedByPullRequestsReferences") or {}
    numbers = [node.get("number") for node in (refs.get("nodes") or []) if node and node.get("number")]
    if (refs.get("pageInfo") or {}).get("hasNextPage"):
        numbers.append(None)
    return numbers


def link_pr(issue_id, pr_id):
    payload = graphql(LINK_MUTATION, {"issueId": issue_id, "prId": pr_id})
    issue = ((payload.get("data") or {}).get("addCloseIssueReferences") or {}).get("issue") or {}
    refs = issue.get("closedByPullRequestsReferences") or {}
    return [node.get("number") for node in (refs.get("nodes") or []) if node and node.get("number")]


def run(issue_number, repo, dry_run=False):
    owner, name = split_repo(repo)
    issue = load_issue(owner, name, issue_number)
    if not issue:
        log(f"#{issue_number} не issue этого репозитория (или это PR). Пропуск.")
        return 0
    if issue.get("state") != "CLOSED":
        log(f"#{issue_number} не закрыта. Пропуск.")
        return 0
    existing = closed_by_numbers(issue)
    if existing:
        shown = ", ".join(f"#{n}" for n in existing if n)
        log(f"#{issue_number} уже имеет closedBy ({shown or 'есть связь'}). Пропуск.")
        return 0

    prs = load_merged_prs(owner, name)
    best = pick_best(prs, issue_number)
    if not best:
        log(f"#{issue_number}: смерженный PR не найден (Dependabot не считается). Пропуск.")
        return 0

    rank = candidate_rank(best, issue_number)
    log(
        f"#{issue_number}: выбран PR #{best['number']} "
        f"(ветка {best['head']}, очки {rank[0]}, смержен {best['merged_at']})."
    )
    if dry_run:
        log("Пробный запуск: мутация не вызывалась.")
        return 0
    if not best.get("id") or not issue.get("id"):
        log("Нет node id задачи или PR.")
        return 1

    try:
        linked = link_pr(issue["id"], best["id"])
    except GraphqlError as exc:
        again = load_issue(owner, name, issue_number)
        if best["number"] in [n for n in closed_by_numbers(again) if n]:
            log(f"#{issue_number}: связь с PR #{best['number']} уже стоит.")
            return 0
        log(f"Не удалось связать #{issue_number} с PR #{best['number']}: {exc}")
        return 1

    if best["number"] not in linked:
        log(
            f"Мутация прошла, но PR #{best['number']} не виден в closedByPullRequestsReferences "
            f"(сейчас: {', '.join('#' + str(n) for n in linked) or 'пусто'})."
        )
        return 1
    log(f"#{issue_number}: closedBy указывает на PR #{best['number']}.")
    return 0


def parse_args(argv):
    parser = argparse.ArgumentParser(description="Связать закрытую задачу со смерженным PR (closedBy).")
    parser.add_argument("--issue-number", type=int, default=None)
    parser.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY") or os.environ.get("REPO") or "")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args(argv)
    if args.issue_number is None:
        raw = os.environ.get("ISSUE_NUMBER", "").strip()
        if raw:
            args.issue_number = int(raw)
    return args


def main(argv=None):
    args = parse_args(sys.argv[1:] if argv is None else argv)
    if not args.issue_number:
        log("Не задан номер задачи (ISSUE_NUMBER или --issue-number).")
        return 1
    try:
        return run(args.issue_number, args.repo, dry_run=args.dry_run)
    except GraphqlError as exc:
        log(f"Ошибка GraphQL: {exc}")
        return 1
    except RuntimeError as exc:
        log(str(exc))
        return 1


if __name__ == "__main__":
    sys.exit(main())
