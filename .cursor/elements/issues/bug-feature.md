# Bug-feature
A Feature with label `bug`. Same columns, same page sections, a `Feature-` page. Opened only for a substantial change: add or remove a field, change a screen, or change a business rule. Decision table: [bugs](../../lifecycle/bugs.md).

- **Template:** [bug-feature-issue](../../templates/github/bug-feature-issue.md); page [feature-page](../../templates/wiki/feature-page.md).
- **Parent:** the current epic when a feature of that epic caused it; the [standing backlog #8](../../project.md#standing-backlog) when unrelated.
- **«Связи»:** the feature that spawned it and the parent epic.
- **Scope:** only that product change; no unrelated fields.
- **Order:** bug-features of the current epic are finished before the next roadmap epic; standing backlog #8 bug-features do not jump ahead of the current epic, except the one repair PR while the queue is stopped ([repair](../../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)).
