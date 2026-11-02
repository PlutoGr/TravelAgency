# 08 Test cases (column Тест-кейсы)
Before development, always.

**Entry:** feature architecture accepted ([07](07-feature-architecture.md)).

**Who:** one cloud agent with the [test designer brief](../briefs/test-designer.md); the orchestrator reviews and publishes.

**Section «Тест-кейсы»** on the `Feature-` page: each case has id (`ТК-1`), precondition, steps, expected result, level (unit / integration / e2e / ручная на dev). Derive every case from the Given/When/Then and from the architecture API and error paths. Cover every criterion. Mark which cases become automated. A gap goes to «Решения», never closed in silence.

An epic has no «Тест-кейсы» section; owner epic acceptance uses «Бизнес-анализ» ([13](13-epic-acceptance.md)). An older epic page that already has one keeps it as is.

**Exit gate:** «Полное ТЗ» holds: Аналитика, Архитектура and Тест-кейсы visible, no empty «Выбрано» for this slice ([gates](README.md#gates)). Then set `статус: разработка` ([09](09-development.md)).
