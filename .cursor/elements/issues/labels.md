# Labels
Created in the repo before first use by the orchestrator, who alone sets them; the matching Status options on the board are project settings (ask the owner first, [board](../board/README.md)). Label descriptions on GitHub follow the «Applies to» column; outdated ones are updated in the docs-and-tasks phase.

## Status labels (one per open issue)
| Label | Column | Applies to |
|---|---|---|
| `статус: очередь` | Очередь | all; nobody is on it |
| `статус: бизнес-анализ` | Бизнес-анализ | epic |
| `статус: архитектура` | Архитектура | epic. A feature found with it moves forward by the [in-flight rule](../../lifecycle/README.md#gates) |
| `статус: разбиение` | Разбиение | epic |
| `статус: тест-кейсы` | Тест-кейсы | Крупная feature with `Тесты: сначала сценарии`, including bug-feature. Every other feature goes to Разработка at [take](../../lifecycle/README.md#take) and does not take the label. An item already in this column under the [in-flight rule](../../lifecycle/README.md#gates) keeps the label |
| `статус: в работе` | В работе | epic only: children in flight |
| `статус: разработка` | Разработка | feature, including bug-feature; non-product issue: PR open, post-merge Deploy wait, and the criteria check when no dev check is needed; includes PR review and the wait for Deploy |
| `статус: приёмка` | Приёмка | feature, including bug-feature; non-product when dev acceptance applies; on dev after Deploy green |
| `статус: демо` | Демо | epic owner demo only (not mid-stage questions; not features) |
| `статус: готово` | Готово | closed after acceptance or as not planned |
| `статус: удержание` | Удержание | paused |

Column rules: [columns](../board/columns/README.md). Set only a label whose «Applies to» includes the issue. An epic never gets `статус: тест-кейсы`, `статус: разработка` or `статус: приёмка`. A feature or bug-feature never gets `статус: архитектура`, `статус: бизнес-анализ`, `статус: разбиение`, `статус: в работе` or `статус: демо`. Create the label `статус: демо` in the repo before first use; the board Status option «Демо» is a project setting and waits for the owner's yes ([orchestrator §6](../../orchestrator.md#6-powers-act-and-report-vs-ask-first)); until it exists, an epic whose last child closed stays in В работе ([epic demo](../../lifecycle/13-epic-acceptance.md)). Label `статус: аналитика` is deleted (was the retired feature column). Retire obsolete `статус: на ревью` once no open card uses it. Do not retire `статус: архитектура`: epics still use it.

## Type labels (one on create)
| Label | Use |
|---|---|
| `тип: родительская` | epic (with field «Тип» = «Эпик») |
| `enhancement` | feature; infra non-product issue (filed as a blank issue; the orchestrator adds `enhancement` + `статус: очередь` when bringing it to the template) |
| `bug` | bug-feature |
| `documentation` | process, rules, wiki non-product issue |
| `question` | question non-product issue |

Optional area labels `область: <тема>` (for example `область: агрегация`) group work by product theme; they are not a type.

`тип: бэклог` is not a route; no other label, title prefix or wiki level adds a type.
