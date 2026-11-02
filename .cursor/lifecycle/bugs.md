# Bugs and backlog items
There is no separate bug type. Decide by size and relation. Every product item walks the **same FSM**; there is no shorter route and no skipped column. For a tiny fix the orchestrator may fill a thin «Бизнес и сценарии» (and later sections) himself, then advance.

| Case | Where it goes | Route |
|---|---|---|
| Typo, or a 500 caused by a typo, inside an open feature | that feature, no new issue, no new page | fixed in Разработка or Приёмка of that feature; extend its test cases if a regression check is needed |
| Substantial change (field, screen, business rule) caused by a feature of the current epic | bug-feature under the current epic | full feature route ([06](06-feature-analytics.md) to [12](12-docs-after-merge.md)), label `bug` |
| Unrelated bug or tiny feature that does not belong to a product epic | child of the [standing backlog](../project.md#standing-backlog) (#8) | same feature route (thin stages OK), label `bug` or `enhancement` |
| Red CI / failed Deploy on `main`, or unowned Dependabot red CI | backlog item (or under current epic if it clearly belongs there) | FSM; take promptly; thin analytics OK |
| Validated security vulnerability | backlog or current epic as fits | same FSM, thin analytics filled quickly by the orchestrator; no special shortcut column |

- A bug-feature is a Feature with label `bug`: same columns, same wiki sections, a `Feature-` page. In «Связи» it links the feature that spawned it and the parent epic or backlog ([bug-feature](../elements/issues/bug-feature.md)).
- A bug-feature covers its product change only; no unrelated fields.
- The current epic is finished including the bug-features spawned by its features before the next roadmap epic.
- The standing backlog holds accidental bugs and small items to sort later; when taken, an item may become its own epic on the roadmap.
- Unprioritized backlog children do not outrank the current epic's work ([queue priority](../elements/board/queue-priority.md)).
