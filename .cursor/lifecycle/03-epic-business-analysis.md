# 03 Epic business analysis (column Бизнес-анализ)
Fix what the owner wants on the exit of this epic. Objectivity: why, what is realistic now, what is later, what is a stub, what we refuse. Not a field catalog and not permission to start feature code.

**Entry:** epic in Очередь taken in the agreed order ([epic pick](02-roadmap.md)). Set `статус: бизнес-анализ` with the take comment ([status comments](../templates/github/status-comments.md)). Intake already recorded changeable initial wishes on the page ([intake](01-intake.md)).

**Who:** the orchestrator with the owner, live, in Russian. That dialogue is the work. A cloud analyst is not the default author. A cloud agent is only a narrow helper (peer-pattern notes, or a [screen mock](#screen-mock) scaffold) under a short brief ([analyst](../briefs/analyst.md)). It does not receive a dump of the methodology and it does not own the section.

**Page** ([epic page template](../templates/wiki/epic-page.md)):
- «Исходные пожелания» stays the intake sketch. It is not the exit truth. Edit it when the owner changes the wish.
- «Ожидания»: the result on the exit; what is realistic now, later, a stub, or refused; «Не делаем»; «Эталон» ([screen mock](#screen-mock), or «нет» when no screen changes). Keep a problem note or a non-goal only when the next stages need it. Do not require the old six blocks (named analogues, a continuum of options, a chosen-story essay). A legacy page that still has «Бизнес и сценарии» is read as «Ожидания» until the later docs phase.
- Open choices sit in «Решения».

**Exit gate:** «Ожидания» states the exit result, what is out, and, when the epic changes screens, «Эталон» naming the approved mock. No field catalog and no services. A choice only the owner can make stays in this column with a normal `Question` until answered. When the bar holds, publish (or leave ready) and ask for [owner stage approval](../orchestrator.md#owner-stage-approval): `Question` = `approve BA #NN`. The live dialogue is that gate; the token records the yes. On yes, clear that `Question` and set `статус: архитектура` ([epic Архитектура](04-epic-architecture.md)). On no or edits, stay, keep that `Question`, rework, and ask again.

## Screen mock
Applies to every epic whose result changes what the user sees on screens, and to a feature opened outside a split that does ([take](README.md#take)).
- **What:** a local clickable mock of the changed pages only: static HTML/CSS/JS, fake data, the backend stubbed inside the mock (no real API). Self-contained: `index.html` opens from disk, relative paths, no build, no external network. No real personal data and no secrets ([security](../rules/security.mdc)).
- **How:** the orchestrator builds it on the box (a helper may scaffold it, [analyst](../briefs/analyst.md)) and iterates it live with the owner until he is satisfied. That approved mock is «Эталон». No extra stage and no separate token: the epic still leaves by `approve BA #NN`. A feature opened outside a split gets its mock inside [take](README.md#take) and stays in Очередь until the owner is satisfied; that wait is a normal `Question`, not a stage approval.
- **Where:** in the wiki repo, folder `mocks/<NN>/` (`NN` = the issue that owns it, entry `index.html`), pushed with the section publish ([wiki](../elements/wiki/README.md)). «Эталон» names that path and the pages it covers. A later change to an approved mock is a «Решения» row with the owner's choice and a republish of the same folder.
- **Use:** at [Разбиение](05-epic-split.md) each UI feature it covers links it on its page; it is that feature's visual acceptance reference ([developer](../briefs/developer.md)). It is a reference, not code to ship, and it does not replace test cases.
