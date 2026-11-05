# Development lifecycle
From an owner idea to a feature accepted on the server and documented. Board columns are the route stages and equal the `статус:*` labels; the label is the source of truth ([board](../elements/board/README.md)). The wiki holds section content only.

## Flow
```
Owner idea / roadmap sketch
  → Intake (epic: initial wishes) → Roadmap upkeep
  → Epic:    Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → Демо → Готово
                                                  ↓ one Feature per split line
  → Feature, size, kind, test policy and the thin «что меняем» at Разбиение
    ([size](05-epic-split.md#size), [test policy](05-epic-split.md#kind-and-test-policy)).
    Take checks that line ([take](#take)). A feature opened outside a split gets those fields in the take turn:
       Крупная + Тесты «сначала сценарии»:
            Очередь → Тест-кейсы → Разработка → Приёмка → Готово
       otherwise (Малая, or Крупная with «без новых» / «поправить существующие»):
            Очередь → Разработка → Приёмка → Готово
             `Doc-`: only when the thin skeleton must change. Малая draft in the PR;
             Крупная draft after merge. Otherwise «вики не затронута».
             Готово only when every named Doc- page is current, or the PR says «вики не затронута».
  → Epic done → STOP: owner demo in Демо against fixed expectations → next epic pick
```
Interrupts: Удержание (long work-stream pause). Owner questions: pause in place (same column) + [memory](../elements/memory.md). Демо = epic owner demo only ([epic demo](13-epic-acceptance.md)); features never use Демо. Per-transition deliverable review stays an FSM rule (not a column). Bugs and backlog: [bugs.md](bugs.md). Process, infra and question issues: [non-product-work.md](non-product-work.md). Continuous duties: [events.md](events.md), [weekly-audit.md](weekly-audit.md), [memory](../elements/memory.md).

## Stages
File numbers only order the files and may appear in link text. Prose names a stage by its column, never by a number.

| # | Stage | Column (item) | Done by (brief) | File |
|---|---|---|---|---|
| 01 | Intake and discussion with the owner | none yet, ends in Очередь | orchestrator | [01-intake.md](01-intake.md) |
| 02 | Roadmap upkeep and epic pick | none | orchestrator, owner confirms | [02-roadmap.md](02-roadmap.md) |
| 03 | Epic business analysis | Бизнес-анализ (epic) | orchestrator with the owner; [analyst](../briefs/analyst.md) only as a narrow helper | [03-epic-business-analysis.md](03-epic-business-analysis.md) |
| 04 | Epic architecture | Архитектура (epic) | [architect](../briefs/architect.md) | [04-epic-architecture.md](04-epic-architecture.md) |
| 05 | Epic split into features | Разбиение (epic) | [architect](../briefs/architect.md) when large; orchestrator when simple | [05-epic-split.md](05-epic-split.md) |
| 08 | Test cases | Тест-кейсы (Крупная + «сначала сценарии») | [test designer](../briefs/test-designer.md) | [08-test-cases.md](08-test-cases.md) |
| 09 | Development | Разработка (feature) | [developer](../briefs/developer.md) or [devops](../briefs/devops.md) | [09-development.md](09-development.md) |
| 10 | Review and merge | Разработка (feature) | automations, orchestrator; [reviewer](../briefs/reviewer.md) on escalation | [10-review-and-merge.md](10-review-and-merge.md) |
| 11 | Deploy and acceptance on dev | Разработка → Приёмка (feature) | orchestrator (dev access) | [11-deploy-and-acceptance.md](11-deploy-and-acceptance.md) |
| 12 | `Doc-` after merge (only when the PR named a page) | right after merge (Малая) or Разработка / Приёмка before Готово (Крупная) | Малая: orchestrator publishes the PR draft; Крупная: [docs writer](../briefs/docs-writer.md), orchestrator publishes | [12-docs-after-merge.md](12-docs-after-merge.md) |
| 13 | Epic completion and owner demo | В работе → Демо → Готово (epic) | orchestrator, owner accepts | [13-epic-acceptance.md](13-epic-acceptance.md) |

Files 06 and 07 are removed. A feature has no column Аналитика and no column Архитектура. Expectations, the etalon and the architecture are fixed on the epic. Per child, `Размер`, `Вид`, `Тесты` and the thin «что меняем» are fixed at split. [Take](#take) checks that line and chooses the next column.

## Take
The single definition of how a feature or bug-feature leaves Очередь. Other files link here. There is no feature column Аналитика and no feature column Архитектура. The check is inside the take turn. It is not an owner question and not a stage-approval token.

1. A child of a split: the split line already carries `Размер`, `Вид`, `Тесты`, the thin «что меняем», and the «Эталон» link when the feature changes screens ([split](05-epic-split.md)). Confirm those are on the issue and the Feature page. Copy a missing field from the split line in this turn. If the split line itself lacks one of those fields, write it in this turn from the epic page and the [size](05-epic-split.md#size) rule. Do not ask the owner about that check. If screens change and neither the line nor the epic names an «Эталон», stay in Очередь with a normal `Question` until that mock exists.
2. A feature opened outside a split (intake, an epic-demo gap, a bug-feature, a backlog child): in this turn write on the issue and the Feature page the thin TZ («Бизнес и сценарии»: what changes and why it meets the epic «Ожидания»), `Размер` ([size](05-epic-split.md#size)), `Вид`, `Тесты`, and «Эталон» when the result changes screens (otherwise «нет»). Link the parent epic mock when it already shows the target; otherwise build a [screen mock](03-epic-business-analysis.md#screen-mock) under `mocks/<this NN>/`. A contradiction with the fixed «Ожидания» is an owner question: stay in Очередь. A new mock stays in Очередь with a normal `Question` until the owner is satisfied; that is not a stage approval. Then continue this take.
3. Малая with `Тесты: сначала сценарии`: write the short scenario list in «Тест-кейсы» in this turn. Every other feature that will not enter Тест-кейсы: «Тест-кейсы» is the policy line (which existing tests change, or `без новых`).
4. Then set the label. Крупная with `Тесты: сначала сценарии` → `статус: тест-кейсы`. Every other feature → `статус: разработка`. The stage comment names that column. The issue does not take `статус: аналитика` or `статус: архитектура`.
5. If this is the epic's first child leaving Очередь, set the epic to `статус: в работе` with `Waiting on` = `children` (never for #8). A child of an epic in Удержание is not taken ([Удержание](../elements/board/columns/on-hold.md)).

The developer's and the test designer's architecture input is the epic «Архитектура». The feature input is the split line and the Feature page TZ. A child that needs architecture the epic block does not cover is an [epic architecture gap](04-epic-architecture.md#gap), not a feature column.

## Gates
- Hard and sequential (**FSM**): [orchestrator §2](../orchestrator.md#2-process-as-a-finite-state-machine). An epic enters every epic column. A feature or bug-feature enters Разработка, Приёмка and Готово, and enters Тест-кейсы only when it is Крупная with `Тесты: сначала сценарии` ([take](#take), [test policy](05-epic-split.md#kind-and-test-policy)). It does not enter Аналитика or Архитектура. Expectations, the etalon, the architecture, and per child `Размер`, `Вид`, `Тесты` and the thin «что меняем» are fixed on the epic. Size is the visible part of [review §3](10-review-and-merge.md#3-escalation-review), recorded at split ([size](05-epic-split.md#size)). Do not copy that list. A stage the route enters is still filled (empty text is not allowed). An item already on a retired feature column moves forward on the new route (in-flight bullet).
- **In-flight items (rules first):** an item already in work when this rule set took effect is not walked back. A feature already in Тест-кейсы, Разработка, Приёмка or Готово keeps that column. It is not given a size it does not have, and a missing `Вид` or `Тесты` does not stop work already in progress, including a feature already in Тест-кейсы. On its next touch the orchestrator maps a feature whose label is off the new route (`статус: аналитика`, feature `статус: архитектура`, `статус: на ревью`, `статус: в работе`) to the column of the work actually in progress: open PR → Разработка; merged and on dev (the latest successful deployed sha contains the merge, [memory](../elements/memory.md#sha-cell)) → Приёмка; merged but not yet contained → Разработка with `Waiting on` = `Deploy`; otherwise the [take](#take) route (Тест-кейсы only when the item is Крупная and `Тесты` is `сначала сценарии`, else Разработка), filling the thin TZ, `Размер`, `Вид`, `Тесты` and «Эталон» in that turn when the page does not have them. Do not finish the retired columns. A hold whose restore column is «Аналитика», or «Архитектура» on a feature, restores onto that same mapped column ([Удержание](../elements/board/columns/on-hold.md)). The remap is a column change subject to the [Question guard](../elements/memory.md#question-guard): while the row's `Question` is set, only record the target column in `Next`. An epic on `статус: архитектура` stays there. A legacy epic (for example #92) stays in В работе and, once resumed, follows the normal epic machine including [epic demo](13-epic-acceptance.md) (В работе → Демо → Готово); an existing owner `Question` on it stays in force until the owner answers it. Old issues and wiki pages are brought to the current templates in a later docs-and-tasks phase.
- A column is entered only when the previous deliverable is visible (wiki section, test policy or cases, PR, green Deploy) and the orchestrator accepted it. Epic Бизнес-анализ, epic Архитектура and Разбиение also wait for [owner stage approval](../orchestrator.md#owner-stage-approval). Take is not a column: the check in [Take](#take) happens in the same turn as the label it sets.
- **Owner stage approval:** epic Бизнес-анализ, epic Архитектура and Разбиение leave only after the owner's yes to the published or ready section ([owner stage approval](../orchestrator.md#owner-stage-approval)). The approval `Question` blocks the move ([Question](../elements/memory.md#question)). Yes follows that stage's exit. No, or a request to edit, stays in the column. Feature and bug-feature columns advance when the orchestrator accepts the stage. The take check is not an owner question. A real owner choice on a feature still pauses in place as a normal `Question`. Epic Демо keeps the existing demo rules. This gate adds no column. Epic «В работе» remains the epic state while children run. Feature «Разработка» remains the coding column.
- Recorded verification: on every advance the orchestrator posts the stage comment ([status comments](../templates/github/status-comments.md)) naming what was checked and the new column, then sets the label and board Status together. The [deliverable review](../templates/agents/deliverable-review.md) is the internal checklist behind that comment (FSM rule, not a board column).
- A cloud agent never changes `статус:*`, the board or the wiki.
- «Полное ТЗ» before Разработка: the feature's thin «Бизнес и сценарии» (what changes, why it meets the epic «Ожидания»), `Размер`, `Вид`, `Тесты` and «Эталон» when screens change, a pointer to the epic «Архитектура» (that block is the architecture; the feature page has no architecture section), and «Тест-кейсы» (a case table only for Крупная `сначала сценарии`; a short scenario list for Малая `сначала сценарии`; otherwise the policy line). No «Решения» row for this slice has an empty «Выбрано». The epic page alone never unlocks feature code: the thin TZ has to be on the Feature page.
- Tests follow the policy recorded at split, or at take when the feature was opened outside a split. `сначала сценарии` means scenarios, then tests and code in the same PR. `без новых` and `поправить существующие` do not wait on a case table.
- Every feature stays aimed at its epic's «Ожидания». A drift is a rewrite, not a new goal.
- Disagreement (analyst vs architect, agent vs page): options go to «Решения»; the orchestrator decides by the rules, otherwise the owner with pause in place ([decisions log](../elements/wiki/decisions-log.md), [memory](../elements/memory.md)). Do not advance while a stage's owner question is open.
- Order: finish the current epic, including bug-features spawned by its features, before the next roadmap epic. Unrelated bugs go to the [standing backlog](../project.md#standing-backlog) (#8) and do not block. After every child is Готово: STOP for the owner demo in Демо; Готово only after the owner accepts.
- Closed work gets no new documents.
- Non-product work Очередь → Разработка → (Приёмка) → Готово is a second, explicit machine ([non-product-work.md](non-product-work.md)). It is separate from the product routes. Red CI and vulnerabilities stay on the product route for their size, with a thin TZ at take allowed ([bugs.md](bugs.md)).
