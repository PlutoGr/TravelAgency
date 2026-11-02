# 05 Epic split (column Разбиение)
**Entry:** epic architecture accepted ([04](04-epic-architecture.md)).

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews, publishes, creates the child issues and pages.

**Section «Разбиение»** on the `Epic-` page: a feature list, one line for why each feature exists, in delivery order with dependencies. Children are features or bug-features only; no subtask, no mini-feature, no other child type.

**Orchestrator after acceptance**
1. For each line: a `Feature-<Name>` page ([feature page template](../templates/wiki/feature-page.md)) and a child issue ([feature issue](../templates/github/feature-issue.md)), `статус: очередь`, then the GitHub sub-issue link:
   ```bash
   child_id=$(gh api repos/PlutoGr/TravelAgency/issues/<child NN> --jq .id)
   gh api -X POST repos/PlutoGr/TravelAgency/issues/<epic NN>/sub_issues -F sub_issue_id="$child_id"
   ```
   Write the child's issue number into its «Разбиение» line. Same turn: add each child's board card if missing and set Status = Очередь.
2. Set contiguous low «Приоритет» on the children ([queue priority](../elements/board/queue-priority.md)).
3. Sidebar entries for the new pages.

**Exit gate:** every line has a page and an issue. In the same turn that accepts the split deliverable, take the first child (that child into Аналитика, the epic to `статус: в работе`) when nothing blocks it. Only when something blocks it, write memory `Next` = take #NN with `Waiting on` = what blocks it, which is only `owner` or `issue #NN`; the epic stays in Разбиение until that child is taken. When the block clears (owner answer, or the close of that issue by the [close procedure](../elements/board/columns/done.md)), set `Waiting on` = `none`, `Next` = take #NN, and take it in that turn. An epic row never waits on `agent`, `CI+Automations` or `Deploy`. When the first child leaves Очередь, set the epic to `статус: в работе` ([column В работе](../elements/board/columns/in-progress.md)) with memory `Waiting on` = `children` ([vocabulary](../elements/memory.md#waiting-on-values)); no script infers this from children.
