# Epic
A business goal. Route: [lifecycle stages 03-05 and 13](../../lifecycle/README.md#stages).

- **Created by:** the orchestrator, from the roadmap or an owner ask ([intake](../../lifecycle/01-intake.md)). Issue template [epic-issue](../../templates/github/epic-issue.md); page template [epic-page](../../templates/wiki/epic-page.md).
- **Labels and fields:** `тип: родительская`, field «Тип» = «Эпик», `статус: очередь` on create.
- **Page:** `Epic-<Name>` with «Исходные пожелания», «Связи с другими эпиками», «Ожидания», «Архитектура», «Решения» (when a question is open), «Разбиение» ([epic pages](../wiki/epic-pages.md)). No «Тест-кейсы». A legacy «Бизнес и сценарии» is the expectations block until the page is refreshed.
- **Children:** features and bug-features only.
- **Order:** the current product epic ([definition](../board/queue-priority.md#current-epic-first)) is finished, including its bug-features, before the next roadmap epic, unless the owner confirmed a parallel set. After every child is Готово: follow the [epic demo](../../lifecycle/13-epic-acceptance.md) entry order (existing `Question` waits; missing Status «Демо» → `create Status «Демо»` and stay in В работе; otherwise Демо + `Question` = `epic demo`); Готово only after owner accept; then epic pick ([epic pick](../../lifecycle/02-roadmap.md)).
- **Standing backlog:** issue #8 holds unrelated bugs and tiny features ([project](../../project.md#standing-backlog)); not a third route. #8 stays `статус: очередь` for life and never enters Демо. `тип: бэклог` on #8 is legacy until the docs phase.
- **Close:** only after owner acceptance in Демо (or not planned), by the [close procedure](../board/columns/done.md).
