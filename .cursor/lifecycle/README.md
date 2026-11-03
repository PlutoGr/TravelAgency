# Development lifecycle
From an owner idea to a feature accepted on the server and documented. Board columns are the route stages and equal the `статус:*` labels; the label is the source of truth ([board](../elements/board/README.md)). The wiki holds section content only.

## Flow
```
Owner idea / roadmap sketch
  → Intake (epic: initial wishes) → Roadmap upkeep
  → Epic:    Очередь → Бизнес-анализ → Архитектура → Разбиение → В работе → Демо → Готово
                                                  ↓ one Feature per split line
  → Feature, size at Аналитика ([size](06-feature-analytics.md#size)),
    kind and test policy at Разбиение ([test policy](05-epic-split.md#kind-and-test-policy)):
       Крупная + Тесты «сначала сценарии»:
            Очередь → Аналитика → Архитектура → Тест-кейсы → Разработка → Приёмка → Готово
       otherwise (Малая, or Крупная with «без новых» / «поправить существующие»):
            Очередь → Аналитика → Архитектура (политика тестов в том же шаге) → Разработка → Приёмка → Готово
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
| 06 | Feature analytics | Аналитика (feature) | orchestrator (thin confirm); [analyst](../briefs/analyst.md) only when asked | [06-feature-analytics.md](06-feature-analytics.md) |
| 07 | Feature architecture | Архитектура (feature) | [architect](../briefs/architect.md) for non-trivial back; orchestrator for a front or docs note | [07-feature-architecture.md](07-feature-architecture.md) |
| 08 | Test cases | Тест-кейсы (Крупная + «сначала сценарии») | [test designer](../briefs/test-designer.md) | [08-test-cases.md](08-test-cases.md) |
| 09 | Development | Разработка (feature) | [developer](../briefs/developer.md) or [devops](../briefs/devops.md) | [09-development.md](09-development.md) |
| 10 | Review and merge | Разработка (feature) | automations, orchestrator; [reviewer](../briefs/reviewer.md) on escalation | [10-review-and-merge.md](10-review-and-merge.md) |
| 11 | Deploy and acceptance on dev | Разработка → Приёмка (feature) | orchestrator (dev access) | [11-deploy-and-acceptance.md](11-deploy-and-acceptance.md) |
| 12 | `Doc-` after merge (only when the PR named a page) | right after merge (Малая) or Разработка / Приёмка before Готово (Крупная) | Малая: orchestrator publishes the PR draft; Крупная: [docs writer](../briefs/docs-writer.md), orchestrator publishes | [12-docs-after-merge.md](12-docs-after-merge.md) |
| 13 | Epic completion and owner demo | В работе → Демо → Готово (epic) | orchestrator, owner accepts | [13-epic-acceptance.md](13-epic-acceptance.md) |

## Gates
- Hard and sequential (**FSM**): [orchestrator §2](../orchestrator.md#2-process-as-a-finite-state-machine). An epic enters every epic column. A feature or bug-feature enters Аналитика, Архитектура, Разработка, Приёмка and Готово. Аналитика is a thin confirm; a pure front or docs Архитектура may be a thin note. A short stage is still filled (empty text is not allowed). `статус: тест-кейсы` is taken only for Крупная with `Тесты: сначала сценарии` ([test policy](05-epic-split.md#kind-and-test-policy)). Every other feature passes that column in the architecture move and does not take the label. Size is the visible part of [review §3](10-review-and-merge.md#3-escalation-review), recorded at analytics ([size](06-feature-analytics.md#size)). Do not copy that list. An item already past Аналитика keeps the route it is already on (in-flight bullet).
- **In-flight items (rules first):** an item already in work when this rule set took effect is not walked back. A feature already past Аналитика is not given a size and keeps the route it is already on, including a feature already in Тест-кейсы. On its next touch the orchestrator maps it to the column of the work actually in progress: open PR → Разработка; merged and on dev (the latest successful deployed sha contains the merge, [memory](../elements/memory.md#sha-cell)) → Приёмка; merged but not yet contained → Разработка with `Waiting on` = `Deploy`; otherwise the column of the earliest unfilled section. The remap is a column change subject to the [Question guard](../elements/memory.md#question-guard): while the row's `Question` is set, only record the target column in `Next`. A legacy `статус: на ревью` or `статус: в работе` on a feature is mapped the same way. A row that already has `Question` = `approve feature BA #NN` keeps that legacy stage-approval token until the owner's yes; new work does not set it. A legacy epic (for example #92) stays in В работе and, once resumed, follows the normal epic machine including [epic demo](13-epic-acceptance.md) (В работе → Демо → Готово); an existing owner `Question` on it stays in force until the owner answers it. Old issues and wiki pages are brought to the current templates in a later docs-and-tasks phase.
- A column is entered only when the previous deliverable is visible (wiki section, test policy or cases, PR, green Deploy) and the orchestrator accepted it. Epic Бизнес-анализ, epic Архитектура and Разбиение also wait for [owner stage approval](../orchestrator.md#owner-stage-approval).
- **Owner stage approval:** epic Бизнес-анализ, epic Архитектура and Разбиение leave only after the owner's yes to the published or ready section ([owner stage approval](../orchestrator.md#owner-stage-approval)). The approval `Question` blocks the move ([Question](../elements/memory.md#question)). Yes follows that stage's exit. No, or a request to edit, stays in the column. Feature and bug-feature columns, including Аналитика, advance when the orchestrator accepts the stage. A real owner choice on a feature still pauses in place as a normal `Question`. Epic Демо keeps the existing demo rules. This gate adds no column. Epic «В работе» remains the epic state while children run. Feature «Разработка» remains the coding column.
- Recorded verification: on every advance the orchestrator posts the stage comment ([status comments](../templates/github/status-comments.md)) naming what was checked and the new column, then sets the label and board Status together. The [deliverable review](../templates/agents/deliverable-review.md) is the internal checklist behind that comment (FSM rule, not a board column).
- A cloud agent never changes `статус:*`, the board or the wiki.
- «Полное ТЗ» before Разработка: the feature's thin «Бизнес и сценарии» (what changes, why it meets the epic «Ожидания», `Вид`, `Тесты`), the feature «Архитектура» (a pointer to the epic block is enough for pure front or docs), and «Тест-кейсы» (a case table only for Крупная `сначала сценарии`; a short scenario list for Малая `сначала сценарии`; otherwise the policy line). No «Решения» row for this slice has an empty «Выбрано». The epic page alone never unlocks feature code.
- Tests follow the policy recorded at split and echoed or corrected at Аналитика. `сначала сценарии` means scenarios, then tests and code in the same PR. `без новых` and `поправить существующие` do not wait on a case table.
- Every feature stays aimed at its epic's «Ожидания». A drift is a rewrite, not a new goal.
- Disagreement (analyst vs architect, agent vs page): options go to «Решения»; the orchestrator decides by the rules, otherwise the owner with pause in place ([decisions log](../elements/wiki/decisions-log.md), [memory](../elements/memory.md)). Do not advance while a stage's owner question is open.
- Order: finish the current epic, including bug-features spawned by its features, before the next roadmap epic. Unrelated bugs go to the [standing backlog](../project.md#standing-backlog) (#8) and do not block. After every child is Готово: STOP for the owner demo in Демо; Готово only after the owner accepts.
- Closed work gets no new documents.
- Non-product work Очередь → Разработка → (Приёмка) → Готово is a second, explicit machine ([non-product-work.md](non-product-work.md)). It is separate from the product routes. Red CI and vulnerabilities stay on the product route for their size, with thin sections allowed ([bugs.md](bugs.md)).
