# Bugs
There is no separate bug type. Decide by size and relation:

| Case | Where it goes | Route |
|---|---|---|
| Typo, or a 500 caused by a typo, inside an open feature | that feature, no new issue, no new page | fixed in Разработка or Приёмка of that feature; extend its test cases if a regression check is needed |
| Substantial change (add or remove a field, change a screen, change a business rule) caused by a feature of the current epic | bug-feature under the current epic | full feature route ([06](06-feature-analytics.md) to [12](12-docs-after-merge.md)), label `bug` |
| Unrelated bug found while an epic is in flight | bug-feature under the standing bugs epic | same feature route, label `bug`; does not stop the current epic and does not jump ahead of it |
| Red CI or failed Deploy on `main` | fix PR at once | [events](events.md) |
| Validated security vulnerability | fixed first, ahead of the epic order | [queue priority](../elements/board/queue-priority.md#outside-the-epic-order) |

- A bug-feature is a Feature with label `bug`: same columns, same wiki sections, a `Feature-` page. In «Связи» it links the feature that spawned it and the parent epic ([bug-feature](../elements/issues/bug-feature.md), [template](../templates/github/bug-feature-issue.md)).
- A bug-feature covers its product change only; no unrelated fields.
- The current epic is finished including the bug-features spawned by its features before the next roadmap epic.
- The standing bugs epic holds only unrelated discoveries. It is an ordinary epic with short text and the same epic columns, not a second route.
- Unprioritized bugs do not outrank other unprioritized cards ([queue priority](../elements/board/queue-priority.md)).
