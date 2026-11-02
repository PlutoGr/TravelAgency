#!/usr/bin/env python3
"""Гигиена доски TravelAgency (GitHub Projects v2, PlutoGr, проект 2).

НЕ конечный автомат и НЕ двигает стадии. Метки «статус: …» и поле Status
меняет только оркестратор (см. .cursor/elements/board/README.md).

За один запуск:
  1. Добавляет на доску открытые issues репозитория, которых там нет
     (возвращает из архива, если карточка была заархивирована).
  2. Архивирует карточки задач, закрытых 30 и более дней назад (--no-archive отключает).
  3. Расставляет порядок внутри ТЕКУЩИХ колонок Status (колонку не меняет):
       Готово     сначала недавно закрытые;
       Очередь    по полю «Приоритет» (1 сверху), затем по номеру;
       прочие колонки по номеру.
     Двигаются только карточки не на своём месте внутри колонки.

PR, черновики и архивные карточки не трогаются. Виды доски не меняются.
Метки issues не меняются. Поле Status карточки не меняется.

Запуск:
  python3 board_order.py --dry-run
  python3 board_order.py
  python3 board_order.py --no-archive
Доступ: GH_PROJECT_TOKEN (бокс) или GH_TOKEN (Actions).
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
PRIORITY_NAME = "Приоритет"
ARCHIVE_DAYS = 30
CHUNK = 5
RETRIES = 4

ITEMS_Q = """query($id:ID!,$after:String){ node(id:$id){ ... on ProjectV2 {
 items(first:100, after:$after){ pageInfo{hasNextPage endCursor} nodes{ id type isArchived
  status: fieldValueByName(name:"Status"){ ... on ProjectV2ItemFieldSingleSelectValue { name } }
  prio: fieldValueByName(name:"%s"){ ... on ProjectV2ItemFieldNumberValue { number } }
  content{ __typename
   ... on Issue { id number title state closedAt }
   ... on PullRequest { number title state closedAt }
   ... on DraftIssue { title } } } } } } }""" % PRIORITY_NAME

OPEN_ISSUES_Q = """query($owner:String!,$repo:String!,$after:String){ repository(owner:$owner,name:$repo){
 issues(states:OPEN, first:100, after:$after){ pageInfo{hasNextPage endCursor} nodes{
  id number title state closedAt
  projectItems(first:20, includeArchived:true){ nodes{ id isArchived project{ id } } } } } } }"""

FIELDS_Q = """query($id:ID!){ node(id:$id){ ... on ProjectV2 {
 fields(first:50){ nodes{
  ... on ProjectV2FieldCommon { id name dataType }
  ... on ProjectV2SingleSelectField { options { id name } }
 } } } } }"""


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


def load_prio_field():
    nodes = gql(FIELDS_Q, {"id": PROJECT_ID})["node"]["fields"]["nodes"]
    for f in nodes:
        if f and f.get("name") == PRIORITY_NAME and f.get("dataType") == "NUMBER":
            return f["id"]
    raise RuntimeError(f"на доске нет числового поля «{PRIORITY_NAME}»")


def read_items():
    res = []
    for n in paginate(ITEMS_Q, {"id": PROJECT_ID}, ["node", "items"]):
        c = n.get("content") or {}
        res.append({
            "id": n["id"], "type": n["type"], "archived": n.get("isArchived", False),
            "status": (n.get("status") or {}).get("name"),
            "prio": (n.get("prio") or {}).get("number"),
            "number": c.get("number"), "title": c.get("title", ""),
            "state": c.get("state"), "closedAt": c.get("closedAt"),
        })
    return res


def managed(it):
    return it["type"] == "ISSUE" and not it["archived"] and it["number"] is not None


def missing_issues(items):
    on_board = {i["number"] for i in items if i["type"] == "ISSUE"}
    res = []
    for c in paginate(OPEN_ISSUES_Q, {"owner": OWNER, "repo": REPO}, ["repository", "issues"]):
        if c["number"] in on_board:
            continue
        archived = [p["id"] for p in c["projectItems"]["nodes"]
                    if p["project"]["id"] == PROJECT_ID and p["isArchived"]]
        res.append({
            "number": c["number"], "title": c.get("title", ""), "state": c.get("state"),
            "closedAt": c.get("closedAt"), "content_id": c["id"],
            "archived_item": archived[0] if archived else None,
        })
    return res


def queue_key(i):
    if i["prio"] is not None:
        return (0, i["prio"], i["number"])
    return (1, 0, i["number"])


def desired_order(col, its):
    if col == "Готово":
        return sorted(its, key=lambda i: (i["closedAt"] or "", i["number"]), reverse=True)
    if col == "Очередь":
        return sorted(its, key=queue_key)
    return sorted(its, key=lambda i: i["number"])


def lis_keep(seq):
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


def plan_order(items, archive):
    archives, moves, desired = [], [], {}
    cutoff = (datetime.now(timezone.utc) - timedelta(days=ARCHIVE_DAYS)).strftime("%Y-%m-%dT%H:%M:%SZ")
    cols = []
    for it in items:
        if not managed(it):
            continue
        if archive and it["state"] == "CLOSED" and it["closedAt"] and it["closedAt"] <= cutoff:
            archives.append(it)
        col = it["status"] or "(без колонки)"
        if col not in cols:
            cols.append(col)
    archived_ids = {i["id"] for i in archives}
    for col in cols:
        if col == "(без колонки)":
            continue
        cur = [i for i in items if managed(i) and i["status"] == col and i["id"] not in archived_ids]
        d = desired_order(col, cur)
        desired[col] = d
        pos = {i["id"]: k for k, i in enumerate(d)}
        keep = lis_keep([pos[i["id"]] for i in cur])
        for k, i in enumerate(d):
            if k not in keep:
                moves.append((i, d[k - 1] if k > 0 else None, col))
    return archives, moves, desired


def run_batched(calls):
    for s in range(0, len(calls), CHUNK):
        part = calls[s:s + CHUNK]
        gql("mutation {" + "\n".join(f"m{k}: {c}" for k, c in enumerate(part)) + "}")


def label(i):
    return f"#{i['number']} {i['title']}"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--no-archive", action="store_true")
    a = ap.parse_args()
    tag = "[dry-run] " if a.dry_run else ""
    load_prio_field()  # fail fast if board misconfigured

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

    archives, moves, desired = plan_order(items, not a.no_archive)
    print(f"{tag}карточек на доске: {len(items)}")
    print(f"{tag}архивировать (закрыты {ARCHIVE_DAYS}+ дней): {len(archives)}"
          + (" [отключено --no-archive]" if a.no_archive else ""))
    for it in archives:
        print(f"  {label(it)} (закрыта {it['closedAt']})")
    print(f"{tag}перемещений внутри колонок: {len(moves)}")
    for it, prev, col in moves:
        print(f"  [{col}] #{it['number']} -> " + (f"после #{prev['number']}" if prev else "наверх"))

    if not a.dry_run:
        run_batched([f'archiveProjectV2Item(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}"}}){{clientMutationId}}'
                     for it in archives])
        run_batched([f'updateProjectV2ItemPosition(input:{{projectId:"{PROJECT_ID}",itemId:"{it["id"]}"'
                     + (f',afterId:"{prev["id"]}"' if prev else "") + '}){clientMutationId}'
                     for it, prev, _ in moves])

    if "Очередь" in desired:
        print("\nОчередь, первые 10:")
        for i in desired["Очередь"][:10]:
            p = f"[{i['prio']:g}] " if i["prio"] is not None else ""
            print("  " + p + label(i))


if __name__ == "__main__":
    try:
        main()
    except RuntimeError as e:
        sys.exit(f"остановлено: {e}")
