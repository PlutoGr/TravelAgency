# Board
One private GitHub Project titled «Задачи», at `PlutoGr/projects/2`. One Status field. Every Status option is visible on that board. Columns = route stages = `статус:*` labels. The label on the issue is the FSM state and the source of truth; wiki pages carry no status line.

If the live project title is not «Задачи», the orchestrator renames it to «Задачи». A token that cannot read this private project leaves that rename to the orchestrator.

## Who changes what
- **Only the orchestrator** sets `статус:*` on every enter/advance, sets field «Приоритет» ([queue priority](queue-priority.md)), and sets the board Status to the same column name **in the same step**. Nobody else, including cloud agents, Cursor Automations and workflows.
- Entering a column triggers the stage ([FSM](../../orchestrator.md#2-process-as-a-finite-state-machine), [columns](columns/README.md)).
- Do not move Projects cards or edit view filters except to keep Status in sync with the label just set. Carve-out: the owner explicitly asks.
- Project Status has **no default** option. On intake, the same turn that creates the issue also adds the card if it is missing and sets Status = Очередь ([intake](../../lifecycle/01-intake.md)).

## Board hygiene (deleted / orchestrator-only)
The former board mover / hygiene script and `board-sync` workflow are **removed**. Nothing auto-moves cards, Status, columns or «Приоритет». The orchestrator alone adds missing cards, sets Status with the label, archives when needed, and orders Очередь by «Приоритет» when refreshing the queue. Call any leftover board-helper description “hygiene deleted / orchestrator-only”.

## Rules
- Exactly one `статус:*` on every open issue; a closed issue has only `статус: готово`. Close only by the [close procedure](columns/done.md), never from a PR body.
- Columns and their entry and exit rules: [columns/README.md](columns/README.md). Label spelling: [labels](../issues/labels.md).
- The Status field needs one option per column name (same spelling), including **Демо**. Status options are one shared list, and every option stays visible. The orchestrator asks the owner to create a missing option and does not set the matching label until the option exists. Until «Демо» exists, the first epic that reaches the demo stays in В работе with `Question` = `create Status «Демо»` ([memory](../memory.md#question)) and the owner is asked once to create it ([epic demo](../../lifecycle/13-epic-acceptance.md)). Remove obsolete «На ревью» once live cards no longer use it; a card still on it is mapped by the [in-flight rule](../../lifecycle/README.md#gates).
- Epic cards move only along Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → Демо → Готово, plus Удержание. Feature and bug-feature cards, Крупная: Очередь → Аналитика → Архитектура → Тест-кейсы → Разработка → Приёмка → Готово, plus Удержание. Малая: Очередь → Аналитика → Архитектура (тест-кейсы в том же шаге) → Разработка → Приёмка → Готово, plus Удержание. No new column ([columns](columns/README.md), [size](../../lifecycle/06-feature-analytics.md#size)). Non-product cards keep their short route ([non-product work](../../lifecycle/non-product-work.md)). The orchestrator enforces the route. All Status options stay on this one board.
- Card order: Очередь top = next by «Приоритет», then by issue number; Готово newest closed first; other columns by issue number; Удержание is its own column.
- Leftover `статус: в работе` or `статус: на ревью` on a feature: on the next touch the orchestrator maps it to the column of the work in progress by the [in-flight rule](../../lifecycle/README.md#gates).
- A body line `⏸ На удержании` has no process meaning; hold only by `статус: удержание` ([Удержание](columns/on-hold.md)). Remove the prefix when the issue is next edited.
