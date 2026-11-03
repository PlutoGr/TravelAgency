# 08 Test cases (column Тест-кейсы)
Entered only for a Крупная feature whose `Тесты` is `сначала сценарии` ([kind and test policy](05-epic-split.md#kind-and-test-policy)). Every other feature writes the policy line inside [Архитектура](07-feature-architecture.md) (Малая with `сначала сценарии` adds a short scenario list there) and does not take this label.

**Entry:** that Крупная architecture is accepted and no «Решения» row for this slice has an empty «Выбрано» ([decisions log](../elements/wiki/decisions-log.md)). The test designer refuses the column otherwise.

**Who:** one cloud agent with the [test designer brief](../briefs/test-designer.md); the orchestrator reviews and publishes.

**Section «Тест-кейсы»** on the `Feature-` page: stable cases. Each case has id (`ТК-1`), precondition, steps, expected result, level (unit / integration / e2e / ручная на dev), and whether it is automated. Choose the level for this feature; do not add ceremony. Derive cases from the thin TZ and from the API and error paths this feature changes. No fragile cases, including ones tied to a calendar date. If a stable test cannot be written, omit it and say so. A gap goes to «Решения», never closed in silence.

An epic has no «Тест-кейсы» section. Owner epic acceptance compares the product to «Ожидания» and the etalon ([epic demo](13-epic-acceptance.md)).

**Exit gate:** «Полное ТЗ» holds ([gates](README.md#gates)). Then set `статус: разработка` ([Разработка](09-development.md)). This column is not an [owner stage approval](../orchestrator.md#owner-stage-approval) column.
