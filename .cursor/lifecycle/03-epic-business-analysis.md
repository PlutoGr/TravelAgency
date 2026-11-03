# 03 Epic business analysis (column Бизнес-анализ)
Fix what the owner wants on the exit of this epic. Objectivity: why, what is realistic now, what is later, what is a stub, what we refuse. Not a field catalog and not permission to start feature code.

**Entry:** epic in Очередь taken in the agreed order ([epic pick](02-roadmap.md)). Set `статус: бизнес-анализ` with the take comment ([status comments](../templates/github/status-comments.md)). Intake already recorded changeable initial wishes on the page ([intake](01-intake.md)).

**Who:** the orchestrator with the owner, live, in Russian. That dialogue is the work. A cloud analyst is not the default author. A cloud agent is only a narrow helper (peer-pattern notes, or a quick static HTML/CSS mock) under a short brief ([analyst](../briefs/analyst.md)). It does not receive a dump of the methodology and it does not own the section.

**Page** ([epic page template](../templates/wiki/epic-page.md)):
- «Исходные пожелания» stays the intake sketch. It is not the exit truth. Edit it when the owner changes the wish.
- «Ожидания»: the result on the exit; what is realistic now, later, a stub, or refused; «Не делаем»; for a visual epic, «Эталон». Keep a problem note or a non-goal only when the next stages need it. Do not require the old six blocks (named analogues, a continuum of options, a chosen-story essay). A legacy page that still has «Бизнес и сценарии» is read as «Ожидания» until the later docs phase.
- Open choices sit in «Решения».

**Visual:** before this column is left, a visual epic has a static HTML/CSS (or similar) mock of the target look. No backend. The owner iterates. The approved mock's path or link is «Эталон». A helper may scaffold the mock; the orchestrator records the reference.

**Exit gate:** «Ожидания» states the exit result, what is out, and, when the epic is visual, the etalon reference. No field catalog and no services. A choice only the owner can make stays in this column with a normal `Question` until answered. When the bar holds, publish (or leave ready) and ask for [owner stage approval](../orchestrator.md#owner-stage-approval): `Question` = `approve BA #NN`. The live dialogue is that gate; the token records the yes. On yes, clear that `Question` and set `статус: архитектура` ([epic Архитектура](04-epic-architecture.md)). On no or edits, stay, keep that `Question`, rework, and ask again.
