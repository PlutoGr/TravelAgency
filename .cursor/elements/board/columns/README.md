# Columns
Each column = one `статус:*` label = one route stage. The orchestrator moves every item: in one step it sets the `статус:*` label (removing every other `статус:*` label), sets the board Status field to the same column name, and posts the [stage comment](../../../templates/github/status-comments.md). No workflow or script moves columns, Status, or «Приоритет» (board hygiene deleted / orchestrator-only).

Epic: Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → Демо → Готово (+ Удержание).
Feature and bug-feature: Очередь → Аналитика → Архитектура → Тест-кейсы → Разработка → Приёмка → Готово (+ Удержание). Features do not use Демо.
Non-product issue: Очередь → Разработка → (Приёмка) → Готово ([non-product work](../../../lifecycle/non-product-work.md)). Owner questions: pause in place (same column) with the memory `Question` ([Question](../../memory.md#question)).

| Column | Label | Applies to | File |
|---|---|---|---|
| Очередь | `статус: очередь` | all | [queue.md](queue.md) |
| Бизнес-анализ | `статус: бизнес-анализ` | epic | [business-analysis.md](business-analysis.md) |
| Аналитика | `статус: аналитика` | feature, including bug-feature | [analytics.md](analytics.md) |
| Архитектура | `статус: архитектура` | epic; feature, including bug-feature | [architecture.md](architecture.md) |
| Разбиение | `статус: разбиение` | epic | [split.md](split.md) |
| Тест-кейсы | `статус: тест-кейсы` | feature, including bug-feature | [test-cases.md](test-cases.md) |
| В работе | `статус: в работе` | epic only | [in-progress.md](in-progress.md) |
| Разработка | `статус: разработка` | feature, including bug-feature; non-product | [development.md](development.md) |
| Приёмка | `статус: приёмка` | feature, including bug-feature; non-product when dev acceptance applies | [acceptance.md](acceptance.md) |
| Демо | `статус: демо` | epic owner demo only (not mid-stage questions; not features) | [demo.md](demo.md) |
| Готово | `статус: готово` | all | [done.md](done.md) |
| Удержание | `статус: удержание` | all | [on-hold.md](on-hold.md) |

Common to every column: one label at a time; advance only through the exit gate of the current column; a cloud agent never moves anything.
