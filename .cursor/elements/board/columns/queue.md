# Очередь (`статус: очередь`)
- **Applies to:** epic, feature, bug-feature, non-product issue. The standing backlog (#8) stays here for life.
- **Enter:** on create (the only label at creation, plus a type label). Same turn: add the card if missing and set board Status = Очередь ([intake](../../../lifecycle/01-intake.md)). Project Status has no default.
- **While here:** nobody works on it. Field «Приоритет» ranks it ([queue priority](../queue-priority.md)).
- **Leave:** on take, in priority order: epic → Бизнес-анализ; feature → Аналитика; non-product → Разработка. Never to a generic in-progress label. When a feature leaves, its product epic goes to В работе if not already there (#8 never moves). A child of an epic in Удержание is never taken, and a held epic's column never changes here ([Удержание](on-hold.md)).
- **Who moves:** orchestrator, with the take comment ([status comments](../../../templates/github/status-comments.md)).
- **Required:** body from the issue template; wiki/page link in «Связи» for **epic and feature** cards only (not non-product); «Приоритет» set.
