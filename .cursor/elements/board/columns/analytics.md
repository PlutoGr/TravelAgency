# Аналитика (`статус: аналитика`)
- **Applies to:** feature, bug-feature.
- **Enter:** take from Очередь (sets the epic to В работе if this is its first child in flight; never #8 — the standing backlog stays in Очередь for life).
- **While here:** the orchestrator writes the thin confirm «Бизнес и сценарии» (what changes, `Вид`, `Тесты`, size). A cloud analyst is not the default ([Аналитика](../../../lifecycle/06-feature-analytics.md)).
- **Leave:** → Архитектура when that thin confirm is accepted, with `Размер`, `Вид` and `Тесты` recorded ([size](../../../lifecycle/06-feature-analytics.md#size), [test policy](../../../lifecycle/05-epic-split.md#kind-and-test-policy)). No owner stage approval. A real owner choice stays here as a normal `Question` ([memory](../../memory.md#question)). A legacy `approve feature BA #NN` still waits for yes.
- **Who moves:** orchestrator, with the stage comment.
- **Required:** the thin section is on the `Feature-` page; `Размер`, `Вид` and `Тесты` are on the issue and the page.
