# Bugs and backlog items
There is no separate bug type. Decide by relation, and by size at analytics ([size](06-feature-analytics.md#size)). Малая uses the short feature route. Крупная uses the full feature route. There is no third route. For a tiny fix the orchestrator may fill a thin «Бизнес и сценарии» (and the later sections of that route) himself, then advance.

| Case | Where it goes | Route |
|---|---|---|
| Typo, or a 500 caused by a typo, inside an open feature | that feature, no new issue, no new page | fixed in Разработка or Приёмка of that feature; extend its test cases if a regression check is needed |
| Substantial change (field, screen, business rule) caused by a feature of the current epic | bug-feature under the current epic | feature route for its size ([Аналитика](06-feature-analytics.md) to [Doc- after merge](12-docs-after-merge.md)), label `bug` |
| Unrelated bug or tiny feature that does not belong to a product epic | child of the [standing backlog](../project.md#standing-backlog) (#8) | same feature route (thin stages OK), label `bug` or `enhancement` |
| Failed Deploy on `main` (or `cancelled` with no later run) | one re-run when nothing is broken; otherwise exactly one repair item: backlog, or the current epic when the failure belongs there | FSM; open or take in the wake that sees the failure, ahead of epic children; thin analytics OK; only its PR merges while the queue is stopped ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped)) |
| Dependabot `pr-checks` result red | bug issue from the [bug template](../templates/github/bug-feature-issue.md), child of the standing backlog | feature route once taken; thin analytics OK; the queue is not stopped. A green Dependabot PR, including a green major, stays on [dependencies](../elements/dependencies.md) |
| Validated security vulnerability | backlog or current epic as fits | same FSM, thin analytics filled quickly by the orchestrator; no special shortcut column |

- A bug-feature is a Feature with label `bug`: same wiki sections, the feature route for its size, a `Feature-` page. In «Связи» it links the feature that spawned it and the parent epic or backlog ([bug-feature](../elements/issues/bug-feature.md)).
- A bug-feature covers its product change only; no unrelated fields.
- The current epic is finished including the bug-features spawned by its features before the next roadmap epic.
- The standing backlog holds accidental bugs and small items to sort later; when taken, an item may become its own epic on the roadmap.
- Unprioritized backlog children do not outrank the current epic's work ([queue priority](../elements/board/queue-priority.md)).
