# Columns
Each column = one `статус:*` label = one route stage. The orchestrator moves every item: in one step it sets the `статус:*` label (removing every other `статус:*` label), sets the board Status field to the same column name, and posts the [stage comment](../../../templates/github/status-comments.md). No workflow or script moves columns, Status, or «Приоритет» (board hygiene deleted / orchestrator-only).

Epic: Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → Демо → Готово (+ Удержание).
Feature and bug-feature, Крупная: Очередь → Аналитика → Архитектура → Тест-кейсы → Разработка → Приёмка → Готово (+ Удержание).
Feature and bug-feature, Малая: Очередь → Аналитика → Архитектура (тест-кейсы в том же шаге) → Разработка → Приёмка → Готово (+ Удержание). No new column: the move from Архитектура to Разработка passes Тест-кейсы ([size](../../../lifecycle/06-feature-analytics.md#size)).
Non-product issue: Очередь → Разработка → (Приёмка) → Готово ([non-product work](../../../lifecycle/non-product-work.md)). Owner questions: pause in place (same column) with the memory `Question` ([Question](../../memory.md#question)).

One project «Задачи», one Status field, one shared option list, every option visible ([board](../README.md)). The orchestrator moves each card only along its route. An epic uses only Очередь, Бизнес-анализ, Архитектура, Разбиение, В работе, Демо, Готово and Удержание. A feature or bug-feature uses only Очередь, Аналитика, Архитектура, Тест-кейсы, Разработка, Приёмка, Готово and Удержание. An epic never receives `статус: аналитика`, `статус: тест-кейсы`, `статус: разработка` or `статус: приёмка`. A feature or bug-feature never receives `статус: бизнес-анализ`, `статус: разбиение`, `статус: в работе` or `статус: демо`. Архитектура is the shared column name; the epic exit and the feature exit stay different ([Архитектура](architecture.md)).

| Column | Label | Applies to | File |
|---|---|---|---|
| Очередь | `статус: очередь` | all | [queue.md](queue.md) |
| Бизнес-анализ | `статус: бизнес-анализ` | epic | [business-analysis.md](business-analysis.md) |
| Аналитика | `статус: аналитика` | feature, including bug-feature | [analytics.md](analytics.md) |
| Архитектура | `статус: архитектура` | epic; feature, including bug-feature | [architecture.md](architecture.md) |
| Разбиение | `статус: разбиение` | epic | [split.md](split.md) |
| Тест-кейсы | `статус: тест-кейсы` | Крупная feature, including bug-feature | [test-cases.md](test-cases.md) |
| В работе | `статус: в работе` | epic only | [in-progress.md](in-progress.md) |
| Разработка | `статус: разработка` | feature, including bug-feature; non-product | [development.md](development.md) |
| Приёмка | `статус: приёмка` | feature, including bug-feature; non-product when dev acceptance applies | [acceptance.md](acceptance.md) |
| Демо | `статус: демо` | epic owner demo only (not mid-stage questions; not features) | [demo.md](demo.md) |
| Готово | `статус: готово` | all | [done.md](done.md) |
| Удержание | `статус: удержание` | all | [on-hold.md](on-hold.md) |

Common to every column: one label at a time; advance only through the exit gate of the current column; leaving epic Бизнес-анализ, epic Архитектура or Разбиение also waits for [owner stage approval](../../../orchestrator.md#owner-stage-approval); feature and bug-feature columns advance when the orchestrator accepts the stage; a cloud agent never moves anything.
