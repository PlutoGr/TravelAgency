# Board
GitHub Project (private) «PlutoGr/projects/2». One Status field. Columns = route stages = `статус:*` labels. The label on the issue is the source of truth; there is no second, coarser status scale; wiki pages carry no status line.

## Who changes what
- The orchestrator sets `статус:*` on every transition and field «Приоритет» ([queue priority](queue-priority.md)). Nobody else, including cloud agents and Cursor Automations.
- `board-sync.yml` (`.github/scripts/board_order.py`; on issue events, daily, manual) copies the label to the Status column, aligns labels with open/closed, orders cards, adds missing issues, archives items closed 30+ days, clears «Приоритет» when a card leaves Очередь.
- Do not move Projects cards, edit the Status field by hand, or edit view filters. Carve-out: the owner explicitly asks.

## Rules
- Exactly one `статус:*` on every open issue; a closed issue has only `статус: готово`, set in the same moment as close. Close only together with `статус: готово`, never from a PR body.
- Columns and their entry and exit rules: [columns/README.md](columns/README.md). Label spelling: [labels](../issues/labels.md).
- The Status field needs one option per column name (same spelling). Until an option exists, `board_order.py` leaves that card's column unchanged and the workflow exits with an error after the rest of the sync. Do not invent option ids in the script; it reads them by name. Create the GitHub label before first use.
- Views: «Задачи» (all except epics, by field «Тип») and «Эпики» («Тип» = «Эпик»). Every column is listed on both; empty columns on a view are expected. Ordinary tasks have no «Тип».
- Card order: Очередь top = next by «Приоритет», then by issue number; Готово newest closed first; other columns by issue number; Удержание is its own column. A hold card still in Очередь (option missing) sorts last.
- One-time relabel: an open feature still on the old coarse `статус: в работе` gets its real column (usually Разработка or Приёмка); the script does not guess.
