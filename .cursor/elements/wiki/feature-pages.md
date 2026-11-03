# Feature pages
`Feature-<Name>` for every feature and bug-feature, created at the epic split or intake. Template: [feature page](../../templates/wiki/feature-page.md). Sidebar group «Фичи». Links its epic; does not repeat the epic text.

| Section | Filled in column | Bar |
|---|---|---|
| «Бизнес и сценарии» | Аналитика | [Аналитика](../../lifecycle/06-feature-analytics.md) |
| «Архитектура» | Архитектура | [feature Архитектура](../../lifecycle/07-feature-architecture.md) |
| «Решения» | whenever a question is open | [decisions log](decisions-log.md) |
| «Тест-кейсы» | Тест-кейсы when Крупная and `Тесты: сначала сценарии`; otherwise the same Архитектура step (a policy line, or a short scenario list for Малая with that policy) | [Тест-кейсы](../../lifecycle/08-test-cases.md) |

- «Полное ТЗ» is defined once in [gates](../../lifecycle/README.md#gates): thin TZ, feature architecture, and the test section the policy requires. The epic page alone does not unlock code. The header carries `Размер` (written at Аналитика), `Вид` and `Тесты`.
- Fill the current column's section, plus «Решения» rows when a question is open; do not fill later sections. If only the owner can decide, pause in place (same column) and record it in [memory](../memory.md). No status line. No «Разбиение» or subtask table (no subtasks).
- One page per open issue; a shared page is split when either issue is next taken.
- A tiny fix inside an open feature adds no new section; it may extend the test cases.
- «сделано» with links (`Doc-`, ADR, PRs) is added in the same step as the close ([done](../board/columns/done.md)).
