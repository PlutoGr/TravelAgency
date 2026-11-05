# Тест-кейсы (`статус: тест-кейсы`)
- **Applies to:** Крупная feature, including bug-feature, whose `Тесты` is `сначала сценарии`. Every other feature goes to Разработка at [take](../../../lifecycle/README.md#take) and does not take the label ([test policy](../../../lifecycle/05-epic-split.md#kind-and-test-policy)). An item already in this column when this rule set took effect stays and is finished here ([in-flight](../../../lifecycle/README.md#gates)).
- **Enter:** from Очередь by that take, only for that case.
- **While here:** a test-designer cloud agent drafts stable «Тест-кейсы»; the orchestrator reviews and publishes ([Тест-кейсы](../../../lifecycle/08-test-cases.md)).
- **Leave:** → Разработка when «Полное ТЗ» holds. Owner question: stay here, `Question` in [memory](../../memory.md#question).
- **Who moves:** orchestrator, with the stage comment.
- **Required:** stable cases for the behavior this feature changes; automated ones marked; no fragile date-tied case; no empty «Выбрано» for this slice.
