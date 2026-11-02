# «Решения» (decision log)
A section on Epic and Feature pages. Not a column, not a task type. Template: [decisions table](../../templates/wiki/decisions-table.md).

- **Row:** Вопрос | Варианты | Выбрано | Почему | Дата (`2026-10-21`). Append only; a changed decision is a new row referencing the old one.
- **What goes in:** every open question from analysis or architecture, every disagreement, every important architecture choice. A silent gap is forbidden before Разработка. On an epic, rows that block Бизнес-анализ, Архитектура or Разбиение.
- **Blocking:** an empty «Выбрано» blocks the column the question belongs to; for a feature slice it blocks Разработка (do not enter, do not keep coding).
- **Disagreement path:** the disagreeing side writes options with «Выбрано» empty and hands back to the orchestrator ([question](../../templates/agents/question.md)). The orchestrator decides by the rules; otherwise one owner question with options and a recommendation, issue in На ревью naming the column to restore ([owner question](../../templates/owner/owner-question.md)).
- **Owner answer:** recorded impersonally in the row and on the issue («**Итог:** решено: ...»); restore the recorded column.
- Once a row has «Выбрано», or the owner already put the task in work, a matching PR needs no second yes at merge.
- A cloud agent returns the options table; it never decides an approval item and never publishes.
