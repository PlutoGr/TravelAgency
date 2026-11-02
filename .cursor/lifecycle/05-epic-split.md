# 05 Epic split (column Разбиение)
**Entry:** epic architecture accepted ([04](04-epic-architecture.md)).

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews, publishes, creates the child issues and pages.

**Section «Разбиение»** on the `Epic-` page: a feature list, one line for why each feature exists, in delivery order with dependencies. Children are features or bug-features only; no subtask, no mini-feature, no other child type.

**Orchestrator after acceptance**
1. For each line: a `Feature-<Name>` page ([feature page template](../templates/wiki/feature-page.md)) and a child issue ([feature issue](../templates/github/feature-issue.md)) linked to the epic, `статус: очередь`, one owner per child.
2. Set contiguous low «Приоритет» on the children ([queue priority](../elements/board/queue-priority.md)).
3. Sidebar entries for the new pages.

**Exit gate:** every line has a page and an issue. When the first child leaves Очередь, set the epic to `статус: в работе` ([column В работе](../elements/board/columns/in-progress.md)); `board-sync` does not infer this from children.
