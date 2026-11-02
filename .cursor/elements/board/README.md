# Board
GitHub Project (private) «PlutoGr/projects/2». One Status field. Columns = route stages = `статус:*` labels. The label on the issue is the FSM state and the source of truth; wiki pages carry no status line.

## Who changes what
- **Only the orchestrator** sets `статус:*` on every enter/advance, sets field «Приоритет» ([queue priority](queue-priority.md)), and sets the board Status to the same column name. Nobody else, including cloud agents, Cursor Automations and workflows.
- Entering a column triggers the stage ([FSM](../../orchestrator.md#2-process-as-a-finite-state-machine), [columns](columns/README.md)).
- Do not move Projects cards or edit view filters except to keep Status in sync with the label just set. Carve-out: the owner explicitly asks.

## Board hygiene
Workflow `board-sync.yml` runs `.github/scripts/board_order.py` on a schedule and manually. It is **not** part of the FSM. It may only:
- add open issues that are missing from the board;
- archive items closed 30+ days;
- order cards **within** their current Status column (Очередь by «Приоритет», Готово newest closed first, others by issue number).

It must **never** change a `статус:*` label, never change the Status column of a card, never clear «Приоритет», and never treat a body prefix as a hold. A failed hygiene run is seen at the weekly audit or the next wake.

## Rules
- Exactly one `статус:*` on every open issue; a closed issue has only `статус: готово`. Close only by the [close procedure](columns/done.md), never from a PR body.
- Columns and their entry and exit rules: [columns/README.md](columns/README.md). Label spelling: [labels](../issues/labels.md).
- The Status field needs one option per column name (same spelling). Missing options are created by the orchestrator before first use; Status options are project settings, so ask the owner first.
- Views: «Задачи» (all except epics, by field «Тип») and «Эпики» («Тип» = «Эпик»). Every column is listed on both; empty columns on a view are expected. Ordinary tasks have no «Тип».
- Card order: Очередь top = next by «Приоритет», then by issue number; Готово newest closed first; other columns by issue number; Удержание is its own column.
- Leftover coarse `статус: в работе` on a feature: the orchestrator relabels it by hand to its real column when that issue is next taken.
- A body line `⏸ На удержании` has no process meaning; hold only by `статус: удержание` ([Удержание](columns/on-hold.md)). Remove the prefix when the issue is next edited.
