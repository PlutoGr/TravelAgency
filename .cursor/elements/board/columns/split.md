# Разбиение (`статус: разбиение`)
- **Applies to:** epic only.
- **Enter:** from Архитектура.
- **While here:** a large or tangled epic is split by an architect cloud agent; a simple epic is split by the orchestrator. Each line has `Вид` and `Тесты`. The orchestrator publishes and creates the child pages and issues ([Разбиение](../../../lifecycle/05-epic-split.md)).
- **Leave:** → В работе after owner yes to `approve split #NN`, when the first child leaves Очередь, in that yes turn ([Разбиение](../../../lifecycle/05-epic-split.md)).
- **Owner question:** stay in this column; record the `Question` in [memory](../../memory.md#question); do not advance until the answer is recorded.
- **Who moves:** orchestrator.
- **Required:** each split line has `Вид`, `Тесты`, a `Feature-` page and a child issue in Очередь with «Приоритет»; each UI feature the epic «Эталон» covers links it on its page.
