# Labels
Created in the repo before first use. Only the orchestrator sets them.

## Status labels (one per open issue)
| Label | Column | Applies to |
|---|---|---|
| `статус: очередь` | Очередь | all; nobody is on it |
| `статус: бизнес-анализ` | Бизнес-анализ | epic |
| `статус: аналитика` | Аналитика | feature, bug-feature |
| `статус: архитектура` | Архитектура | epic, feature |
| `статус: разбиение` | Разбиение | epic |
| `статус: тест-кейсы` | Тест-кейсы | feature, bug-feature |
| `статус: в работе` | В работе | epic only: children in flight |
| `статус: разработка` | Разработка | feature, bug-feature, non-product; includes PR review and the wait for Deploy |
| `статус: приёмка` | Приёмка | feature, bug-feature; on dev after Deploy green |
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
