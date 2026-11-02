"""Решение, отправлять ли один webhook CI/CD, и сборка payload. Сеть не нужна."""

import json
import re
import sys

# Обязательные проверки PR: задачи «CI» и «Secret scan».
# Cursor Automations в этот список не входят.
DEFAULT_REQUIRED = (
    ".NET сборка и тесты",
    "Фронтенд сборка и тесты",
    "Скрипт чипа PR",
    "gitleaks",
)

_SHA = re.compile(r"^[0-9a-f]{40}$")


def required_names(extra=None):
    names = []
    for name in list(DEFAULT_REQUIRED) + list(extra or []):
        if name and name not in names:
            names.append(name)
    return names


def _stamp(run):
    if run.get("status") == "completed":
        return run.get("completed_at") or ""
    return run.get("started_at") or run.get("completed_at") or ""


def _suite_id(run):
    suite = run.get("check_suite") or {}
    value = suite.get("id", run.get("check_suite_id"))
    if value is None or value == "":
        return None
    return int(value)


def latest_by_name(check_runs, names, sha):
    latest = {}
    allowed = set(names)
    for run in check_runs or []:
        name = run.get("name")
        if name not in allowed:
            continue
        head = run.get("head_sha")
        if head and sha and head != sha:
            continue
        current = latest.get(name)
        key = (_stamp(run), int(run.get("id") or 0))
        if current is None or key > (_stamp(current), int(current.get("id") or 0)):
            latest[name] = run
    return latest


def _winner(latest):
    return max(
        latest.values(),
        key=lambda run: (run.get("completed_at") or "", int(run.get("id") or 0)),
    )


def decide_pr(check_runs, extra, sha, pr_head, suite_id):
    """skip, либо send с полями result и failed_checks.

    suite_id это check_suite_id запуска «CI» или «Secret scan».
    Одно событие на завершение: шлёт только suite с самой поздней
    обязательной проверкой. Чужой sha пропускается.
    """
    if not sha or sha != pr_head:
        return {"action": "skip", "reason": "superseded"}
    try:
        trigger = int(suite_id)
    except (TypeError, ValueError):
        return {"action": "skip", "reason": "not-latest-suite"}
    names = required_names(extra)
    latest = latest_by_name(check_runs, names, sha)
    for name in names:
        run = latest.get(name)
        if run is None or run.get("status") != "completed":
            return {"action": "skip", "reason": "incomplete"}
    winner = _winner(latest)
    if _suite_id(winner) != trigger:
        return {"action": "skip", "reason": "not-latest-suite"}
    failed = [
        name
        for name in names
        if latest[name].get("conclusion") != "success"
    ]
    return {
        "action": "send",
        "result": "red" if failed else "green",
        "failed_checks": failed,
    }


def pr_payload(decision, pr, sha, branch, author, html_url):
    return {
        "source": "github-actions",
        "event": "pr-checks",
        "result": decision["result"],
        "pr": int(pr),
        "sha": sha,
        "branch": branch,
        "author": author,
        "failed_checks": decision["failed_checks"],
        "html_url": html_url,
    }


def deploy_payload(conclusion, sha, deployed_sha, branch, run_id, html_url):
    green = conclusion == "success"
    sha_ok = bool(deployed_sha) and bool(_SHA.match(deployed_sha))
    return {
        "source": "github-actions",
        "event": "deploy",
        "result": "green" if green else "red",
        "conclusion": conclusion or "",
        "sha": sha,
        "deployed_sha": deployed_sha if green and sha_ok else "",
        "branch": branch,
        "run_id": str(run_id),
        "html_url": html_url,
    }


def _pr_from_stdin(data):
    decision = decide_pr(
        data.get("checks") or [],
        data.get("extra") or [],
        data.get("sha") or "",
        data.get("pr_head") or "",
        data.get("suite_id"),
    )
    if decision["action"] == "skip":
        return decision
    decision["payload"] = pr_payload(
        decision,
        data["pr"],
        data.get("sha") or "",
        data.get("branch") or "",
        data.get("author") or "",
        data.get("html_url") or "",
    )
    return decision


def main(argv):
    if len(argv) != 2 or argv[1] not in ("pr", "deploy"):
        print("usage: notify_guard.py pr|deploy", file=sys.stderr)
        return 2
    data = json.load(sys.stdin)
    if argv[1] == "pr":
        out = _pr_from_stdin(data)
    else:
        out = {
            "action": "send",
            "payload": deploy_payload(
                data.get("conclusion") or "",
                data.get("sha") or "",
                data.get("deployed_sha") or "",
                data.get("branch") or "",
                data.get("run_id") or "",
                data.get("html_url") or "",
            ),
        }
    json.dump(out, sys.stdout)
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
