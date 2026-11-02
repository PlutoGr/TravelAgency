# Очередь (`статус: очередь`)
- **Applies to:** epic, feature, bug-feature, non-product issue.
- **Enter:** on create (the only label at creation, plus a type label) ([intake](../../../lifecycle/01-intake.md)).
- **While here:** nobody works on it. Field «Приоритет» ranks it ([queue priority](../queue-priority.md)).
- **Leave:** on take, in priority order: epic → Бизнес-анализ; feature → Аналитика; non-product → Разработка. Never to a generic in-progress label. When a feature leaves, its epic goes to В работе if not already there.
- **Who moves:** orchestrator, with the take comment ([status comments](../../../templates/github/status-comments.md)).
- **Required:** body from the issue template, page link in «Связи» (epic, feature), «Приоритет» set.
