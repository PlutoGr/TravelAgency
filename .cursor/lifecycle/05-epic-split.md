# 05 Epic split (column Разбиение)
**Entry:** epic architecture accepted ([epic Архитектура](04-epic-architecture.md)).

**Who:** a large or tangled epic is one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews and publishes. A simple epic is split by the orchestrator. Either way the orchestrator creates the child issues and pages.

**Section «Разбиение»** on the `Epic-` page: a feature list, one line for why each feature exists, in delivery order with dependencies. Each line carries `Размер`, `Вид`, `Тесты` and the thin «что меняем» ([size](#size), [below](#kind-and-test-policy)). The feature body says what changes and why it serves «Ожидания», plus links to siblings. It is not a second business analysis. Each `фронт` feature (and the UI part of any other) that the epic «Эталон» covers names that mock and its pages in the page «Эталон» line; that mock is its visual acceptance reference ([screen mock](03-epic-business-analysis.md#screen-mock)). Children are features or bug-features only; no subtask, no mini-feature, no other child type.

## Kind and test policy
Recorded here, on each split line, and copied onto the child issue and the Feature page. [Take](README.md#take) checks the line. It does not invent a second analysis.

- `Вид`: `фронт`, `бэк` or `документация`. Another kind is a rules change. This is a recorded tag, not a `статус:*` and not a board column.
- `Тесты`: exactly one of `без новых`, `поправить существующие`, `сначала сценарии`.
  - `без новых`: no new automated tests. Usual for docs, copy and visual polish.
  - `поправить существующие`: adjust the tests named on the feature.
  - `сначала сценарии`: an important new API. Крупная writes the cases in Тест-кейсы; Малая writes a short scenario list at [take](README.md#take), not in a column. Then tests first and code in the same PR. The brief forbids fitting tests to bad code.
- A stable test that cannot be written is omitted. A fragile test, including one tied to a calendar date, is forbidden.
- `статус: тест-кейсы` is taken only for Крупная with `сначала сценарии` ([Тест-кейсы](08-test-cases.md)). Every other feature goes to Разработка at take with a visible policy line and does not take the label.

A bug-feature opened later (acceptance miss or epic-demo gap) gets `Размер`, `Вид`, `Тесты`, the thin TZ and «Эталон» at [take](README.md#take), not a second split. Often `без новых` or `поправить существующие`; new tests only when a stable check is worth it ([bugs](bugs.md)).

## Size
Recorded on each split line, and copied onto the child issue and the Feature page, as `Размер: Малая` or `Размер: Крупная`. A feature opened outside a split gets the same line at [take](README.md#take). Use only the visible part of [review §3](10-review-and-merge.md#3-escalation-review): Крупная when that section already calls the work Крупная, otherwise Малая. Do not copy the list. Writing the epic «Архитектура» is not a trigger for a child. An item already in Тест-кейсы, Разработка, Приёмка or Готово when this rule set took effect is not sized and keeps the column it is already on ([in-flight](README.md#gates)).

Малая uses a `Doc-` draft in the PR only when the skeleton needs it, published right after merge. Крупная with `Тесты: сначала сценарии` enters Тест-кейсы. Any other feature does not. A later escalation on a Малая head does not change `Размер` or the route ([review §3](10-review-and-merge.md#3-escalation-review)). Both reach Готово only when every named `Doc-` page is current, or the PR says «вики не затронута».

**Orchestrator after acceptance**
1. For each line: a `Feature-<Name>` page ([feature page template](../templates/wiki/feature-page.md)) and a child issue ([feature issue](../templates/github/feature-issue.md)) with `Размер`, `Вид`, `Тесты` and the thin «что меняем», `статус: очередь`, then the GitHub sub-issue link:
   ```bash
   child_id=$(gh api repos/PlutoGr/TravelAgency/issues/<child NN> --jq .id)
   gh api -X POST repos/PlutoGr/TravelAgency/issues/<epic NN>/sub_issues -F sub_issue_id="$child_id"
   ```
   Write the child's issue number into its «Разбиение» line. Same turn: add each child's board card if missing and set Status = Очередь.
2. Set contiguous low «Приоритет» on the children ([queue priority](../elements/board/queue-priority.md)).
3. Sidebar entries for the new pages.

These steps leave every child in Очередь and leave the epic in Разбиение.

**Exit gate:** every line has `Размер`, `Вид`, `Тесты`, the thin «что меняем», a page and an issue; every UI feature the epic «Эталон» covers links it. Publish that section in the acceptance turn. Do not take the first child and do not set `статус: в работе` in that turn. When no other `Question` is open, ask for [owner stage approval](../orchestrator.md#owner-stage-approval): `Question` = `approve split #NN`, `Waiting on` = `none`, `Next` = take the first child on yes. On no or edits, stay in Разбиение, keep that `Question`, rework, and ask again. On yes, clear that `Question`, then take the first child by [take](README.md#take) (that child into Тест-кейсы or Разработка, the epic to `статус: в работе`) when nothing blocks it; the epic row gets `Waiting on` = `children` ([vocabulary](../elements/memory.md#waiting-on-values)). When the first child is blocked: an owner question goes in the epic's `Question` ([Question](../elements/memory.md#question)); a blocking issue creates the child's own row still in Очередь (`Waiting on` = `issue #NN`, `Next` = take the child; [Create](../elements/memory.md#row-lifecycle)), and the epic row waits on `children` in Разбиение. When the block clears (answer turn, or the close of that issue by the [close procedure](../elements/board/columns/done.md)), take the child in that turn. Any take of a child (including one created by an interim demo remark), subject to the [Question guard](../elements/memory.md#question-guard) on the child's and the epic's rows, moves the epic to `статус: в работе` in the same turn with `Waiting on` = `children` ([column В работе](../elements/board/columns/in-progress.md)); a blocked child stays in Очередь with its own wait. Later children are taken by the [next child](../elements/memory.md#waiting-on-values) rule. After this exit the epic row waits on `children`, not on `agent`, `Automations`, or `Deploy`; no script infers the epic column from children.
