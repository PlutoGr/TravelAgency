# Epic pages
`Epic-<Name>` (Latin, English, Title-Case-With-Hyphens), created at intake with the issue. Template: [epic page](../../templates/wiki/epic-page.md). Sidebar group «Фичи».

| Section | Filled in column | Bar |
|---|---|---|
| «Исходные пожелания» | intake, edited in Бизнес-анализ when the wish changes | [intake](../../lifecycle/01-intake.md) |
| «Связи с другими эпиками» | Бизнес-анализ or Архитектура | [epic Архитектура](../../lifecycle/04-epic-architecture.md) |
| «Ожидания» | Бизнес-анализ | [Бизнес-анализ](../../lifecycle/03-epic-business-analysis.md) |
| «Архитектура» | Архитектура | [epic Архитектура](../../lifecycle/04-epic-architecture.md) |
| «Решения» | whenever a question is open | [decisions log](decisions-log.md) |
| «Разбиение» | Разбиение | [Разбиение](../../lifecycle/05-epic-split.md) |

- The working blocks are wishes, then expectations (with «Эталон» when the epic changes screens), then architecture, then the feature list. A legacy heading «Бизнес и сценарии» is «Ожидания» until the page is next edited.
- Fill the current column's section, plus «Решения» rows when a question is open; do not fill later sections. If only the owner can decide, pause in place (same column) and record it in [memory](../memory.md). No status line. No «Тест-кейсы» section.
- Not a screen specification; does not unlock feature code. «Эталон» is a clickable mock reference in `mocks/<NN>/` ([screen mock](../../lifecycle/03-epic-business-analysis.md#screen-mock)), not feature code. No child analysis pages: the child is a Feature.
- «сделано» with links is added in the same step as the close after owner acceptance ([done](../board/columns/done.md)).
