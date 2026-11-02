# Columns
Each column = one `статус:*` label = one route stage. The orchestrator moves every item (sets the label and posts the [stage comment](../../../templates/github/status-comments.md)); `board-sync` moves the card.

Epic: Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → На ревью → Готово (+ Удержание).
Feature and bug-feature: Очередь → Аналитика → Архитектура → Тест-кейсы → Разработка → Приёмка → Готово (+ На ревью, Удержание).
Non-product issue: Очередь → Разработка → (Приёмка) → Готово (+ На ревью) ([non-product work](../../../lifecycle/non-product-work.md)).

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
| На ревью | `статус: на ревью` | all (owner decision); epic owner acceptance | [on-review.md](on-review.md) |
| Готово | `статус: готово` | all | [done.md](done.md) |
| Удержание | `статус: удержание` | all | [on-hold.md](on-hold.md) |

Common to every column: one label at a time; advance only through the exit gate of the current column; a cloud agent never moves anything.
