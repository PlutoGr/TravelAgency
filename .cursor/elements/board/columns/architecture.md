# Архитектура (`статус: архитектура`)
- **Applies to:** epic and feature (bug-feature too).
- **Enter:** epic from Бизнес-анализ ([epic Архитектура](../../../lifecycle/04-epic-architecture.md)); feature from Аналитика ([feature Архитектура](../../../lifecycle/07-feature-architecture.md)). Refuse an epic while «Ожидания» are missing. Refuse a feature while the thin confirm, size and test policy are missing, except an in-flight feature that keeps the route it is already on ([in-flight](../../../lifecycle/README.md#gates)).
- **While here:** epic, and a non-trivial backend feature: an architect cloud agent drafts «Архитектура». A pure front or docs feature: the orchestrator writes a thin note. The orchestrator publishes.
- **Leave:** epic → Разбиение after owner yes to `approve epic architecture #NN` ([epic Архитектура](../../../lifecycle/04-epic-architecture.md)); Крупная feature with `Тесты: сначала сценарии` → Тест-кейсы; every other feature → Разработка in the same move, with the policy line (or a short scenario list) already on the page ([feature Архитектура](../../../lifecycle/07-feature-architecture.md)). Owner question: stay here, `Question` in [memory](../../memory.md#question).
- **Who moves:** orchestrator, with the stage comment.
- **Required:** architecture section visible; every important choice logged in «Решения».
