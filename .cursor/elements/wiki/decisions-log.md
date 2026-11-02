# «Решения» (decision log)
A section on Epic and Feature pages. Not a column, not a task type. Template: [decisions table](../../templates/wiki/decisions-table.md).

- **Row:** Вопрос | Варианты | Выбрано | Почему | Дата (`2026-10-21`). Append only; a changed decision is a new row referencing the old one.
- **What goes in:** every open question from analysis or architecture, every disagreement, every important architecture choice. A silent gap is forbidden before Разработка. On an epic, rows that block Бизнес-анализ, Архитектура or Разбиение.
- **Blocking:** do not leave a stage and do not launch a later stage while a «Выбрано» for that stage is empty when only the owner can fill it, or when the next stage would waste work (for example do not launch Тест-кейсы while an architecture choice is open). Open rows that the next column can settle may travel with the page; the orchestrator says so in the stage comment. On a feature, every row for this slice must have «Выбрано» before Тест-кейсы and before Разработка («Полное ТЗ»). On an epic, empty rows block Разбиение when the split depends on them.
- **Owner question:** keep the issue in its **current column** (pause in place); record `Waiting on` = owner in [memory](../memory.md); ask once with options and a recommendation ([owner question](../../templates/owner/owner-question.md)). Remind on the next memory refresh, next day, or weekly audit: not every wake.
- **Disagreement path:** the disagreeing side writes options with «Выбрано» empty and hands back to the orchestrator ([question](../../templates/agents/question.md)). The orchestrator decides by the rules; otherwise owner question with pause in place.
- **Owner answer:** recorded impersonally in the row and on the issue («**Итог:** решено: ...»); clear the pause in memory; continue the same column.
- Once a row has «Выбрано», or the owner already put the task in work, a matching PR needs no second yes at merge.
- A cloud agent returns the options table; it never decides an approval item and never publishes.
