# Board
GitHub Project (private) «PlutoGr/projects/2». One Status field. Columns = route stages = `статус:*` labels. The label on the issue is the source of truth; there is no second, coarser status scale; wiki pages carry no status line.

## Who changes what
- The orchestrator sets `статус:*` on every transition and field «Приоритет» ([queue priority](queue-priority.md)). Nobody else, including cloud agents and Cursor Automations.
- `board-sync.yml` (`.github/scripts/board_order.py`; on issue events, daily, manual) copies the label to the Status column, orders cards, adds missing issues and archives items closed 30+ days. Exactly what it does:
  - a closed issue goes to Готово and gets `статус: готово` (other status labels removed);
  - an open issue with no status label and no column gets `статус: очередь`;
  - «Приоритет» is cleared only when a card leaves Очередь by its single label or by closing;
  - conflicts are not repaired: an open issue with `статус: готово` (for example after reopen), two status labels, or no label while already in a column keep their label and column, and the run fails (`::error::`, exit 2) until the orchestrator fixes the label;
  - a body that starts with `⏸ На удержании` has no effect and is only logged when the label is not `статус: удержание`;
  - it never infers an epic's label from its children and never relabels a leftover coarse `статус: в работе` on a feature.
- A failed board-sync run is seen on the next wake or at the weekly audit; the orchestrator fixes the label it names.
- Do not move Projects cards, edit the Status field by hand, or edit view filters. Carve-out: the owner explicitly asks.

## Rules
- Exactly one `статус:*` on every open issue; a closed issue has only `статус: готово`. Close only by the [close procedure](columns/done.md), never from a PR body.
- Columns and their entry and exit rules: [columns/README.md](columns/README.md). Label spelling: [labels](../issues/labels.md).
- The Status field needs one option per column name (same spelling). Until an option exists, `board_order.py` leaves that card's column unchanged and the workflow exits with an error after the rest of the sync. Do not invent option ids in the script; it reads them by name. Missing options and labels are created by the orchestrator before first use; Status options are project settings, so ask the owner first.
- Views: «Задачи» (all except epics, by field «Тип») and «Эпики» («Тип» = «Эпик»). Every column is listed on both; empty columns on a view are expected. Ordinary tasks have no «Тип».
- Card order: Очередь top = next by «Приоритет», then by issue number; Готово newest closed first; other columns by issue number; Удержание is its own column. A hold card still in Очередь (option missing) sorts last.
- Leftover coarse `статус: в работе` on a feature: the orchestrator relabels it by hand to its real column (usually Разработка or Приёмка); the script does not.
- A hold body prefix found on an issue is removed when the issue is next edited; it never replaces the label.
