# Feature pages
`Feature-<Name>` for every feature and bug-feature, created at the epic split or intake. Template: [feature page](../../templates/wiki/feature-page.md). Sidebar group «Фичи». Links its epic; does not repeat the epic text.

| Section | Filled in column | Bar |
|---|---|---|
| «Бизнес и сценарии» | Аналитика | [stage 06](../../lifecycle/06-feature-analytics.md) |
| «Архитектура» | Архитектура | [stage 07](../../lifecycle/07-feature-architecture.md) |
| «Решения» | whenever a question is open | [decisions log](decisions-log.md) |
| «Тест-кейсы» | Тест-кейсы | [stage 08](../../lifecycle/08-test-cases.md) |

- «Полное ТЗ» = «Бизнес и сценарии» (column Аналитика) + «Архитектура» + «Тест-кейсы», no empty «Выбрано» for this slice.
- Fill the current column's section, plus «Решения» rows when a question is open; do not fill later sections. If only the owner can decide, set На ревью and name the column to restore. No status line. No «Разбиение» or subtask table (no subtasks).
- A tiny fix inside an open feature adds no new section; it may extend the test cases.
- «сделано» with links (`Doc-`, ADR, PRs) is added in the same step as the close ([done](../board/columns/done.md)).
