# Feature pages
`Feature-<Name>` for every feature and bug-feature, created at the epic split or intake. Template: [feature page](../../templates/wiki/feature-page.md). Sidebar group «Фичи». Links its epic; does not repeat the epic text.

| Section | Filled in column | Bar |
|---|---|---|
| «Аналитика» | Аналитика | [stage 06](../../lifecycle/06-feature-analytics.md) |
| «Архитектура» | Архитектура | [stage 07](../../lifecycle/07-feature-architecture.md) |
| «Решения» | whenever a question is open | [decisions log](decisions-log.md) |
| «Тест-кейсы» | Тест-кейсы | [stage 08](../../lifecycle/08-test-cases.md) |

- «Полное ТЗ» = Аналитика + Архитектура + Тест-кейсы, no empty «Выбрано» for this slice.
- Fill only the section of the current column. No status line. No «Разбиение» or subtask table (no subtasks).
- Legacy heading «Бизнес и сценарии» counts as «Аналитика» until the page is next edited, then it is renamed. A legacy «Разбиение» subtask table is removed on the next edit.
- A tiny fix inside an open feature adds no new section; it may extend the test cases.
- After Готово: «сделано» with links (`Doc-`, ADR, PRs).
