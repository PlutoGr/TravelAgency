# Feature pages
`Feature-<Name>` for every feature and bug-feature, created at the epic split or intake. Template: [feature page](../../templates/wiki/feature-page.md). Sidebar group «Фичи». Links its epic; does not repeat the epic text. Architecture is the epic «Архитектура», not a section on this page.

| Section | Filled when | Bar |
|---|---|---|
| «Бизнес и сценарии» | split, or [take](../../lifecycle/README.md#take) when the feature was opened outside a split | the thin TZ: what changes and why it meets the epic «Ожидания» ([split](../../lifecycle/05-epic-split.md)) |
| «Решения» | whenever a question is open | [decisions log](decisions-log.md) |
| «Тест-кейсы» | Тест-кейсы when Крупная and `Тесты: сначала сценарии`; otherwise at take (a policy line, or a short scenario list for Малая with that policy) | [Тест-кейсы](../../lifecycle/08-test-cases.md) |

- «Полное ТЗ» is defined once in [gates](../../lifecycle/README.md#gates): the thin TZ, a pointer to the epic architecture, and the test section the policy requires. The epic page alone does not unlock code. The header carries `Размер` (written at split, or at take when opened outside a split), `Вид`, `Тесты` and «Эталон» ([screen mock](../../lifecycle/03-epic-business-analysis.md#screen-mock)).
- A legacy feature «Архитектура» section is not the architecture input. Drop it when the page is next edited for its own work. Do not extend it.
- Fill the section the current step needs, plus «Решения» rows when a question is open; do not fill later sections. If only the owner can decide, pause in place (same column) and record it in [memory](../memory.md). No status line. No «Разбиение» or subtask table (no subtasks).
- One page per open issue; a shared page is split when either issue is next taken.
- A tiny fix inside an open feature adds no new section; it may extend the test cases.
- «сделано» with links (`Doc-`, ADR, PRs) is added in the same step as the close ([done](../board/columns/done.md)).
