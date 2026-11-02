# Bug-feature issue
Labels: `bug`, `статус: очередь`. Sub-issue of the current epic or of the standing backlog #8 ([project](../../project.md#standing-backlog)). Rules: [bug-feature](../../elements/issues/bug-feature.md). Only for a substantial change (field, screen, business rule).

Title: `исправление(<сервис>): <что исправляется кратко>`.

```markdown
## Зачем
<что сломано или неверно сейчас; шаги воспроизведения на dev; что ожидалось>

## Что сделать
Пройти маршрут фичи с меткой `bug`: аналитика изменения, архитектура, тест-кейсы, разработка, приёмка на dev.

## Критерии приёмки
- [ ] <проверяемое условие>
- [ ] Регрессионный тест-кейс добавлен и пройден на dev
- [ ] Страница `Doc-`, названная в PR, обновлена после слияния (или «вики не затронута»)

## Связи
- Фича-источник: #NN, [Feature-<Name>](https://github.com/PlutoGr/TravelAgency/wiki/Feature-<Name>)
- Эпик: #NN
- Страница: [Feature-<Name>](https://github.com/PlutoGr/TravelAgency/wiki/Feature-<Name>)
```
