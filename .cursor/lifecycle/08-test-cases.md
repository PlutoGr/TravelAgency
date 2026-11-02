# 08 Test cases (column Тест-кейсы)
Before development, always. Крупная only. Малая writes this section inside Архитектура and does not enter this column ([feature Архитектура](07-feature-architecture.md)).

**Entry:** Крупная feature architecture accepted ([feature Архитектура](07-feature-architecture.md)) and no «Решения» row for this slice has an empty «Выбрано» ([decisions log](../elements/wiki/decisions-log.md)). The test designer refuses the column for Малая.

**Who:** one cloud agent with the [test designer brief](../briefs/test-designer.md); the orchestrator reviews and publishes.

**Section «Тест-кейсы»** on the `Feature-` page: each case has id (`ТК-1`), precondition, steps, expected result, level (unit / integration / e2e / ручная на dev). Derive every case from the Given/When/Then and from the architecture API and error paths. Cover every criterion. Mark which cases become automated. A gap goes to «Решения», never closed in silence.

An epic has no «Тест-кейсы» section; owner epic acceptance uses its «Бизнес и сценарии» ([epic demo](13-epic-acceptance.md)).

**Exit gate:** «Полное ТЗ» holds: «Бизнес и сценарии», «Архитектура» and «Тест-кейсы» visible, no empty «Выбрано» for this slice ([gates](README.md#gates)). Then set `статус: разработка` ([Разработка](09-development.md)). [Owner stage approval](../orchestrator.md#owner-stage-approval) applies to epic Бизнес-анализ, epic Архитектура and Разбиение.
