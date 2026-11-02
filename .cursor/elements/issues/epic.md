# Epic
A business goal. Route: [lifecycle stages 03-05 and 13](../../lifecycle/README.md#stages).

- **Created by:** the orchestrator, from the roadmap or an owner ask ([intake](../../lifecycle/01-intake.md)). Issue template [epic-issue](../../templates/github/epic-issue.md); page template [epic-page](../../templates/wiki/epic-page.md).
- **Labels and fields:** `тип: родительская`, field «Тип» = «Эпик», `статус: очередь` on create.
- **Page:** `Epic-<Name>` with sections «Связи с другими эпиками», «Бизнес-анализ», «Архитектура», «Решения» (when a question is open), «Разбиение» ([epic pages](../wiki/epic-pages.md)). No «Тест-кейсы».
- **Children:** features and bug-features only, one owner each.
- **Order:** the current epic (label other than Очередь, Готово, Удержание) is finished, including its bug-features, before the next roadmap epic, unless the owner confirmed a parallel set. After Готово: owner demo and epic pick ([13](../../lifecycle/13-epic-acceptance.md), [02](../../lifecycle/02-roadmap.md)).
- **Standing bugs epic:** an ordinary epic with the same columns and short text that says why this class of work exists; holds only unrelated discoveries. `тип: бэклог` is not a third route.
- **Close:** only after owner acceptance (or not planned), together with `статус: готово`.
