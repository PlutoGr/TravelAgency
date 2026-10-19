#!/usr/bin/env python3
"""Порядок карточек на доске TravelAgency (GitHub Projects v2, PlutoGr, проект 2).

Что делает за один запуск:
  1. Добавляет на доску открытые issues репозитория, которых там нет
     (возвращает из архива, если карточка была заархивирована).
  2. Сверяет колонку Status с состоянием issue: закрытая задача идёт в «Готово»,
     задача на удержании в «Очередь», остальные по метке «статус: …».
     Открытая задача без метки статуса остаётся в своей колонке
     (новая карточка без колонки попадает в «Очередь»).
  3. Очищает поле «Приоритет» у карточек, которые ушли из «Очереди».
  4. Архивирует карточки задач, закрытых 30 и более дней назад (--no-archive отключает).
  5. Расставляет порядок внутри колонок:
       Готово             сначала недавно закрытые;
       Очередь            по полю «Приоритет» (1 сверху), затем открытые баги по номеру,
                          затем остальные по номеру, задачи на удержании в самом низу;
       В работе, На ревью по номеру.
     Двигаются только карточки не на своём месте (наибольшая возрастающая
     подпоследовательность остаётся на месте).
  6. Печатает итог и после реального запуска перечитывает доску и сверяет порядок.

PR, черновики и архивные карточки не двигаются. Метки, issues и виды доски не меняются.

Запуск:
  python3 board_order.py --dry-run     # только показать план
  python3 board_order.py               # применить
  python3 board_order.py --no-archive  # применить без архивации
Доступ: токен с правом project из GH_PROJECT_TOKEN (бокс) или GH_TOKEN (Actions).
Значение токена не печатается. Нужны только стандартная библиотека Python и gh.
"""
import argparse
import json
import os
import re
import subprocess
import sys
import time
from bisect import bisect_left
from datetime import datetime, timedelta, timezone

GH = os.environ.get("GH_BIN", "gh")
OWNER, REPO = "PlutoGr", "TravelAgency"
PROJECT_ID = "PVT_kwHOAwCr0M4BlcPE"
STATUS_FIELD = "PVTSSF_lAHOAwCr0M4BlcPEzhkJHcE"
PRIORITY_NAME = "Приоритет"
OPTIONS = {"Очередь": "f75ad846", "В работе": "47fc9ee4", "На ревью": "87d44386", "Готово": "98236657"}
LABEL_TO_COL = {"статус: очередь": "Очередь", "статус: в работе": "В работе",
                "статус: на ревью": "На ревью", "статус: готово": "Готово",
                "статус: удержание": "Очередь"}
HOLD_LABEL = "статус: удержание"
HOLD_PREFIX = "⏸ На удержании"
ARCHIVE_DAYS = 30
CHUNK = 5          # мутаций в одном запросе: на 20 GitHub отвечал HTTP 502
RETRIES = 4        # повторы при ответе 5xx, пауза 2, 4, 8, 16 с

ITEMS_Q = """query($id:ID!,$after:String){ node(id:$id){ ... on ProjectV2 {
 items(first:100, after:$after){ pageInfo{hasNextPage endCursor} nodes{ id type isArchived
  status: fieldValueByName(name:"Status"){ ... on ProjectV2ItemFieldSingleSelectValue { name } }
  prio: fieldValueByName(name:"%s"){ ... on ProjectV2ItemFieldNumberValue { number } }
  content{ __typename
   ... on Issue { id number title state closedAt body labels(first:50){nodes{name}} }
   ... on PullRequest { number title state closedAt }
   ... on DraftIssue { title } } } } } } }""" % PRIORITY_NAME

OPEN_ISSUES_Q = """query($owner:String!,$repo:String!,$after:String){ repository(owner:$owner,name:$repo){
 issues(states:OPEN, first:100, after:$after){ pageInfo{hasNextPage endCursor} nodes{
  id number title state closedAt body labels(first:50){nodes{name}}
  projectItems(first:20, includeArchived:true){ nodes{ id isArchived project{ id } } } } } } }"""

FIELDS_Q = """query($id:ID!){ node(id:$id){ ... on ProjectV2 {
 fields(first:50){ nodes{ ... on ProjectV2FieldCommon { id name dataType } } } } } }"""


def token():
    tok = os.environ.get("GH_PROJECT_TOKEN") or os.environ.get("GH_TOKEN")
    if not tok:
        sys.exit("Нет токена: задайте GH_PROJECT_TOKEN (бокс) или GH_TOKEN (Actions)")
    return tok


def gql(query, variables=None):
    env = dict(os.environ, GH_TOKEN=token())
    body = json.dumps({"query": query, "variables": variables or {}})
    for attempt in range(RETRIES + 1):
        p = subprocess.run([GH, "api", "graphql", "--input", "-"], input=body, env=env,
                           capture_output=True, text=True)
        out = p.stdout.strip()
        try:
            data = json.loads(out) if out else {}
        except ValueError:
            data = {}
        if p.returncode == 0 and not data.get("errors"):
            return data["data"]
        err = data.get("errors") or p.stderr.strip() or out
        if re.search(r"HTTP 5\d\d", p.stderr) and attempt < RETRIES:
            pause = 2 ** (attempt + 1)
            print(f"  ответ 5xx, повтор через {pause} с", file=sys.stderr)
            time.sleep(pause)
            continue
        raise RuntimeError(f"ошибка GraphQL: {err}")


def paginate(query, variables, path):
    nodes, after = [], None
    while True:
        conn = gql(query, dict(variables, after=after))
        for key in path:
            conn = conn[key]
        nodes.extend(conn["nodes"])
        if not conn["pageInfo"]["hasNextPage"]:
            return nodes
        after = conn["pageInfo"]["endCursor"]


def priority_field_id():
    nodes = gql(FIELDS_Q, {"id": PROJECT_ID})["node"]["fields"]["nodes"]
    for f in nodes:
        if f.get("name") == PRIORITY_NAME and f.get("dataType") == "NUMBER":
            return f["id"]
    raise RuntimeError(f"на доске нет числового поля «{PRIORITY_NAME}»")


def norm_issue(c):
    labels = [l["name"] for l in (c.get("labels") or {}).get("nodes", [])]
    return {"number": c.get("number"), "title": c.get("title", ""), "state": c.get("state"),
            "closedAt": c.get("closedAt"), "labels": labels,
            "body1": (c.get("body") or "").lstrip().split("\n", 1)[0].strip()}


def read_items():
    """Карточки доски в текущем порядке (архивные GitHub в этот список не отдаёт)."""
    res = []
    for n in paginate(ITEMS_Q, {"id": PROJECT_ID}, ["node", "items"]):
        it = norm_issue(n.get("content") or {})
        it.update(id=n["id"], type=n["type"], archived=n.get("isArchived", False),
                  status=(n.get("status") or {}).get("name"),
                  prio=(n.get("prio") or {}).get("number"))
        res.append(it)
    return res


def is_hold(it):
    return HOLD_LABEL in it["labels"] or it["body1"].startswith(HOLD_PREFIX)


def managed(it):
    return it["type"] == "ISSUE" and not it["archived"] and it["number"] is not None


def wanted_status(it, notes):
    """Нужная колонка или None, если колонку не трогать."""
    if it["state"] == "CLOSED":
        return "Готово"
    if is_hold(it):
        return "Очередь"
    cols = {LABEL_TO_COL[l] for l in it["labels"] if l in LABEL_TO_COL}
    if len(cols) > 1:
        notes.append(f"#{it['number']}: противоречивые метки статуса {sorted(cols)}, колонка «{it['status']}» не меняется")
        return None
    if not cols:
        if it["status"] is None:
            return "Очередь"
        notes.append(f"#{it['number']}: открыта без метки статуса, остаётся в «{it['status']}»")
        return None
    col = cols.pop()
    if col == "Готово":
        notes.append(f"#{it['number']}: открыта, но с меткой «статус: готово»")
    return col


def missing_issues(items):
    """Открытые issues репозитория без активной карточки на доске."""
    on_board = {i["number"] for i in items if i["type"] == "ISSUE"}
    res = []
    for c in paginate(OPEN_ISSUES_Q, {"owner": OWNER, "repo": REPO}, ["repository", "issues"]):
        if c["number"] in on_board:
            continue
        archived = [p["id"] for p in c["projectItems"]["nodes"]
                    if p["project"]["id"] == PROJECT_ID and p["isArchived"]]
        it = norm_issue(c)
        it.update(content_id=c["id"], archived_item=archived[0] if archived else None)
        res.append(it)
    return res


def queue_key(i):
    if is_hold(i):
        return (3, 0, i["number"])
    if i["prio"] is not None:
        return (0, i["prio"], i["number"])
    if i["state"] == "OPEN" and "bug" in i["labels"]:
        return (1, 0, i["number"])
    return (2, 0, i["number"])


def desired_order(col, its, notes):
    if col == "Готово":
        return sorted(its, key=lambda i: (i["closedAt"] or "", i["number"]), reverse=True)
    if col == "Очередь":
        for i in its:
            if i["prio"] is not None and is_hold(i):
                notes.append(f"#{i['number']}: задан приоритет {i['prio']:g}, но задача на удержании, она внизу")
        return sorted(its, key=queue_key)
    return sorted(its, key=lambda i: i["number"])


def lis_keep(seq):
    """seq: желаемые индексы в текущем порядке -> индексы, которые остаются на месте."""
    tails, tails_idx, prev = [], [], [-1] * len(seq)
    for k, v in enumerate(seq):
        j = bisect_left(tails, v)
        if j == len(tails):
            tails.append(v)
            tails_idx.append(k)
        else:
            tails[j] = v
            tails_idx[j] = k
        prev[k] = tails_idx[j - 1] if j > 0 else -1
    keep, k = set(), tails_idx[-1] if tails_idx else -1
    while k != -1:
        keep.add(seq[k])
        k = prev[k]
    return keep


def plan(items, archive):
    notes, fixes, clears, archives, moves, desired = [], [], [], [], [], {}
    cutoff = (datetime.now(timezone.utc) - timedelta(days=ARCHIVE_DAYS)).strftime("%Y-%m-%dT%H:%M:%SZ")
    for it in items:
        if not managed(it):
            it["new_status"] = it["status"]
            continue
        w = wanted_status(it, notes)
        it["new_status"] = w if w else it["status"]
        if w and w != it["status"]:
            fixes.append((it, it["status"], w))
        if it["prio"] is not None and it["new_status"] != "Очередь":
            clears.append(it)
        if archive and it["state"] == "CLOSED" and it["closedAt"] and it["closedAt"] <= cutoff:
            archives.append(it)
    archived_ids = {i["id"] for i in archives}
    for col in OPTIONS:
        cur = [i for i in items if managed(i) and i["new_status"] == col and i["id"] not in archived_ids]
        d = desired_order(col, cur, notes)
        desired[col] = d
        pos = {i["id"]: k for k, i in enumerate(d)}
        keep = lis_keep([pos[i["id"]] for i in cur])
        for k, i in enumerate(d):
            if k not in keep:
                moves.append((i, d[k - 1] if k > 0 else None, col))
    return fixes, clears, archives, moves, desired, notes


def run_batched(calls):
    for s in range(0, len(calls), CHUNK):
        part = calls[s:s + CHUNK]
        gql("mutation {" + "\n".join(f"m{k}: {c}" for k, c in enumerate(part)) + "}")


def label(i):
    return f"#{i['number']} {i['title']}"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true", help="только показать план, ничего не менять")
    ap.add_argument("--no-archive", action="store_true", help=f"не архивировать задачи, закрытые {ARCHIVE_DAYS}+ дней назад")
    a = ap.parse_args()
    tag = "[dry-run] " if a.dry_run else ""
    prio_field = priority_field_id()

    items = read_items()
    missing = missing_issues(items)
    print(f"{tag}добавить на доску: {len(missing)}")
    for m in missing:
        print(f"  {label(m)}" + (" (вернуть из архива)" if m["archived_item"] else ""))
    if missing and not a.dry_run:
        unarch = [m for m in missing if m["archived_item"]]
        run_batched([f'unarchiveProjectV2Item(input:{{projectId:"{PROJECT_ID}",itemId:"{m["archived_item"]}"}}){{clientMutationId}}'
                     for m in unarch])
        run_batched([f'addProjectV2ItemById(input:{{projectId:"{PROJECT_ID}",contentId:"{m["content_id"]}"}}){{item{{id}}}}'
                     for m in missing if not m["archived_item"]])
        items = read_items()
    elif missing:
        for k, m in enumerate(missing):  # в dry-run новые карточки условно встают в конец
            m.update(id=f"new-{k}", type="ISSUE", archived=False, status=None, prio=None)
            items.append(m)

    before = {c: sum(1 for i in items if managed(i) and i["status"] == c) for c in OPTIONS}
    fixes, clears, archives, moves, desired, notes = plan(items, not a.no_archive)
    after = {c: len(desired[c]) for c in OPTIONS}
    other = sum(1 for i in items if not managed(i))

    print(f"{tag}карточек на доске: {len(items)} (не трогаются PR, черновики, архив: {other})")
    print(f"{tag}колонки до:    {before}")
    print(f"{tag}колонки после: {after} (архивируется: {len(archives)})")
    print(f"{tag}исправлений колонки: {len(fixes)}")
    for it, old, new in fixes:
        print(f"  {label(it)}: {old} -> {new}")
    print(f"{tag}очистить «{PRIORITY_NAME}»: {len(clears)}")
    for it in clears:
        print(f"  {label(it)} (было {it['prio']:g})")
    print(f"{tag}архивировать (закрыты {ARCHIVE_DAYS}+ дней назад): {len(archives)}"
          + (" [отключено --no-archive]" if a.no_archive else ""))
    for it in archives:
        print(f"  {label(it)} (закрыта {it['closedAt']})")
    print(f"{tag}перемещений: {len(moves)}")
    for it, prev, col in moves:
        print(f"  [{col}] #{it['number']} -> " + (f"после #{prev['number']}" if prev else "наверх"))
    for n in notes:
        print(f"замечание: {n}")

    if not a.dry_run:
        run_batched([f'updateProjectV2ItemFieldValue(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}",'
                     f'fieldId:"{STATUS_FIELD}",value:{{singleSelectOptionId:"{OPTIONS[new]}"}}}}){{clientMutationId}}'
                     for it, _, new in fixes])
        run_batched([f'clearProjectV2ItemFieldValue(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}",'
                     f'fieldId:"{prio_field}"}}){{clientMutationId}}' for it in clears])
        run_batched([f'archiveProjectV2Item(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}"}}){{clientMutationId}}'
                     for it in archives])
        run_batched([f'updateProjectV2ItemPosition(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}"'
                     + (f',afterId:"{prev["id"]}"' if prev else "") + '}){clientMutationId}'
                     for it, prev, _ in moves])
        def check():
            items2 = read_items()
            good = len(items2) == len(items) - len(archives)
            bad_cols = [col for col in OPTIONS
                        if [i["id"] for i in items2 if managed(i) and i["status"] == col]
                        != [i["id"] for i in desired[col]]]
            return good and not bad_cols, bad_cols

        # GitHub Projects применяет изменения с задержкой: при расхождении перечитываем доску ещё раз.
        ok, bad_cols = check()
        for pause in (5, 15):
            if ok:
                break
            time.sleep(pause)
            ok, bad_cols = check()
        for col in bad_cols:
            print(f"СВЕРКА: в колонке «{col}» порядок или состав отличается от плана")
        print("сверка: доска совпадает с планом" if ok else "сверка: РАСХОЖДЕНИЕ")
        if not ok:
            sys.exit(2)

    print("\nОчередь, первые 10:")
    for i in desired["Очередь"][:10]:
        p = f"[{i['prio']:g}] " if i["prio"] is not None else ""
        print("  " + p + label(i) + ("  (удержание)" if is_hold(i) else ""))
    print("Готово, первые 5:")
    for i in desired["Готово"][:5]:
        print(f"  {label(i)}  (закрыта {i['closedAt']})")


if __name__ == "__main__":
    try:
        main()
    except RuntimeError as e:
        sys.exit(f"остановлено: {e}")
