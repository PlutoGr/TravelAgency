# 05 Epic split (column Разбиение)
**Entry:** epic architecture accepted ([epic Архитектура](04-epic-architecture.md)).

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

These steps leave every child in Очередь and leave the epic in Разбиение.

**Exit gate:** every line has a page and an issue. Publish that section in the acceptance turn. Do not take the first child and do not set `статус: в работе` in that turn. When no other `Question` is open, ask for [owner stage approval](../orchestrator.md#owner-stage-approval): `Question` = `approve split #NN`, `Waiting on` = `none`, `Next` = take the first child on yes. On no or edits, stay in Разбиение, keep that `Question`, rework, and ask again. On yes, clear that `Question`, then take the first child (that child into Аналитика, the epic to `статус: в работе`) when nothing blocks it; the epic row gets `Waiting on` = `children` ([vocabulary](../elements/memory.md#waiting-on-values)). When the first child is blocked: an owner question goes in the epic's `Question` ([Question](../elements/memory.md#question)); a blocking issue creates the child's own row still in Очередь (`Waiting on` = `issue #NN`, `Next` = take the child; [Create](../elements/memory.md#row-lifecycle)), and the epic row waits on `children` in Разбиение. When the block clears (answer turn, or the close of that issue by the [close procedure](../elements/board/columns/done.md)), take the child in that turn. Any take of a child (including one created by an interim demo remark), subject to the [Question guard](../elements/memory.md#question-guard) on the child's and the epic's rows, moves the epic to `статус: в работе` in the same turn with `Waiting on` = `children` ([column В работе](../elements/board/columns/in-progress.md)); a blocked child stays in Очередь with its own wait. Later children are taken by the [next child](../elements/memory.md#waiting-on-values) rule. After this exit the epic row waits on `children`, not on `agent`, `Automations`, or `Deploy`; no script infers the epic column from children.
