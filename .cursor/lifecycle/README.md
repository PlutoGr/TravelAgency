# Development lifecycle
From an owner idea to a feature accepted on the server and documented. Board columns are the route stages and equal the `статус:*` labels; the label is the source of truth ([board](../elements/board/README.md)). The wiki holds section content only.

## Flow
```
Owner idea / roadmap
  → 01 Intake and discussion → 02 Roadmap upkeep
  → Epic:    Очередь → Бизнес-анализ (03) → Архитектура (04) → Разбиение (05) → В работе → Демо (13) → Готово
                                                              ↓ one Feature per split line
  → Feature: Очередь → Аналитика (06) → Архитектура (07) → Тест-кейсы (08) → Разработка (09, 10) → Приёмка (11) → Готово
             side condition, only when «Документация» names a page: `Doc-` (12), drafted after merge while in Разработка or Приёмка; Готово waits for it
  → Epic done → STOP: owner demo in Демо (13) → next epic pick (02)
```
Interrupts: Удержание (long work-stream pause). Owner questions: pause in place (same column) + [memory](../elements/memory.md). Демо = epic owner demo only ([epic demo](13-epic-acceptance.md)); features never use Демо. Per-transition deliverable review stays an FSM rule (not a column). Bugs and backlog: [bugs.md](bugs.md). Process, infra and question issues: [non-product-work.md](non-product-work.md). Continuous duties: [events.md](events.md), [weekly-audit.md](weekly-audit.md), [memory](../elements/memory.md).

## Stages
File numbers only order the files and may appear in link text. Prose names a stage by its column, never by a number.

| # | Stage | Column (item) | Done by (brief) | File |
|---|---|---|---|---|
| 01 | Intake and discussion with the owner | none yet, ends in Очередь | orchestrator | [01-intake.md](01-intake.md) |
| 02 | Roadmap upkeep and epic pick | none | orchestrator, owner confirms | [02-roadmap.md](02-roadmap.md) |
| 03 | Epic business analysis | Бизнес-анализ (epic) | [analyst](../briefs/analyst.md) | [03-epic-business-analysis.md](03-epic-business-analysis.md) |
| 04 | Epic architecture | Архитектура (epic) | [architect](../briefs/architect.md) | [04-epic-architecture.md](04-epic-architecture.md) |
| 05 | Epic split into features | Разбиение (epic) | [architect](../briefs/architect.md) | [05-epic-split.md](05-epic-split.md) |
| 06 | Feature analytics | Аналитика (feature) | [analyst](../briefs/analyst.md) | [06-feature-analytics.md](06-feature-analytics.md) |
| 07 | Feature architecture | Архитектура (feature) | [architect](../briefs/architect.md) | [07-feature-architecture.md](07-feature-architecture.md) |
| 08 | Test cases | Тест-кейсы (feature) | [test designer](../briefs/test-designer.md) | [08-test-cases.md](08-test-cases.md) |
| 09 | Development | Разработка (feature) | [developer](../briefs/developer.md) or [devops](../briefs/devops.md) | [09-development.md](09-development.md) |
| 10 | Review and merge | Разработка (feature) | automations, orchestrator; [reviewer](../briefs/reviewer.md) on escalation | [10-review-and-merge.md](10-review-and-merge.md) |
| 11 | Deploy and acceptance on dev | Разработка → Приёмка (feature) | orchestrator (dev access) | [11-deploy-and-acceptance.md](11-deploy-and-acceptance.md) |
| 12 | `Doc-` after merge (only when the PR named a page) | Разработка after merge or Приёмка, before Готово | [docs writer](../briefs/docs-writer.md), orchestrator publishes | [12-docs-after-merge.md](12-docs-after-merge.md) |
| 13 | Epic completion and owner demo | В работе → Демо → Готово (epic) | orchestrator, owner accepts | [13-epic-acceptance.md](13-epic-acceptance.md) |

## Gates
- Hard and sequential (**FSM**): [orchestrator §2](../orchestrator.md#2-process-as-a-finite-state-machine). No column is skipped on a product epic or feature, including small features, bug-features and backlog children. A short stage is still entered and filled (thin text by the orchestrator is allowed). There is no shorter product route.
- **In-flight items (rules first):** an item already in work when this rule set took effect is not walked back. On its next touch the orchestrator maps it to the column of the work actually in progress: open PR → Разработка; merged and on dev (the latest successful deployed sha contains the merge, [memory](../elements/memory.md#sha-cell)) → Приёмка; merged but not yet contained → Разработка with `Waiting on` = `Deploy`; otherwise the column of the earliest unfilled section. A legacy `статус: на ревью` or `статус: в работе` on a feature is mapped the same way. A legacy epic (for example #92) stays in В работе and, once resumed, follows the normal epic machine including [epic demo](13-epic-acceptance.md) (В работе → Демо → Готово); an existing owner `Question` on it stays in force until the owner answers it. Old issues and wiki pages are brought to the current templates in a later docs-and-tasks phase.
- A column is entered only when the previous deliverable is visible (wiki section, test cases, PR, green Deploy) and the orchestrator accepted it.
- Recorded verification: on every advance the orchestrator posts the stage comment ([status comments](../templates/github/status-comments.md)) naming what was checked and the new column, then sets the label and board Status together. The [deliverable review](../templates/agents/deliverable-review.md) is the internal checklist behind that comment (FSM rule, not a board column).
- A cloud agent never changes `статус:*`, the board or the wiki.
- «Полное ТЗ» before Разработка: the Feature page shows «Бизнес и сценарии», «Архитектура» and «Тест-кейсы», and no «Решения» row for this slice has an empty «Выбрано». The epic's «Бизнес и сценарии» alone never unlocks feature code; the epic page is not the feature specification.
- Test cases come before development.
- Every feature stays aimed at its epic's problem. A drift is a rewrite, not a new goal.
- Disagreement (analyst vs architect, agent vs page): options go to «Решения»; the orchestrator decides by the rules, otherwise the owner with pause in place ([decisions log](../elements/wiki/decisions-log.md), [memory](../elements/memory.md)). Do not advance while a stage's owner question is open.
- Order: finish the current epic, including bug-features spawned by its features, before the next roadmap epic. Unrelated bugs go to the [standing backlog](../project.md#standing-backlog) (#8) and do not block. After every child is Готово: STOP for the owner demo in Демо; Готово only after the owner accepts.
- Closed work gets no new documents.
- Non-product work Очередь → Разработка → (Приёмка) → Готово is a second, explicit machine ([non-product-work.md](non-product-work.md)). It is the written exception to “no skipped product column”; red CI and vulnerabilities stay on the product machine with thin sections ([bugs.md](bugs.md)).
