# Labels
Created in the repo before first use by the orchestrator, who alone sets them; the matching Status options on the board are project settings (ask the owner first, [board](../board/README.md)). Label descriptions on GitHub follow the «Applies to» column; outdated ones are updated in the docs-and-tasks phase.

## Status labels (one per open issue)
| Label | Column | Applies to |
|---|---|---|
| `статус: очередь` | Очередь | all; nobody is on it |
| `статус: бизнес-анализ` | Бизнес-анализ | epic |
| `статус: аналитика` | Аналитика | feature, including bug-feature |
| `статус: архитектура` | Архитектура | epic; feature, including bug-feature |
| `статус: разбиение` | Разбиение | epic |
| `статус: тест-кейсы` | Тест-кейсы | feature, including bug-feature |
| `статус: в работе` | В работе | epic only: children in flight |
| `статус: разработка` | Разработка | feature, including bug-feature; non-product while its PR is open; includes PR review and the wait for Deploy |
| `статус: приёмка` | Приёмка | feature, including bug-feature; non-product when dev acceptance applies; on dev after Deploy green |
| `статус: на ревью` | На ревью | owner decision; epic owner acceptance |
| `статус: готово` | Готово | closed after acceptance or as not planned |
| `статус: удержание` | Удержание | paused |

Column rules: [columns](../board/columns/README.md).

## Type labels (one on create)
| Label | Use |
|---|---|
| `тип: родительская` | epic (with field «Тип» = «Эпик») |
| `enhancement` | feature; infra non-product issue |
| `bug` | bug-feature |
| `documentation` | process, rules, wiki non-product issue |
| `question` | question non-product issue |

Optional area labels `область: <тема>` (for example `область: агрегация`) group work by product theme; they are not a type.

`тип: бэклог` is not a route; no other label, title prefix or wiki level adds a type.
