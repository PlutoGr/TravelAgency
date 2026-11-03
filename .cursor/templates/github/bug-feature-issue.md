# Bug-feature issue
Labels: `bug`, `статус: очередь`. Sub-issue of the current epic or of the standing backlog #8 ([project](../../project.md#standing-backlog)). Rules: [bug-feature](../../elements/issues/bug-feature.md). Only for a substantial change (field, screen, business rule).

Title: `исправление(<сервис>): <что исправляется кратко>`.

```markdown
## Зачем
<что сломано или неверно сейчас; шаги воспроизведения на dev; что ожидалось>

## Что сделать
Пройти маршрут фичи с меткой `bug`. Тонкое ТЗ на странице фичи.

Вид: <фронт | бэк | документация>
Размер: <Малая | Крупная>
Тесты: <без новых | поправить существующие | сначала сценарии>

## Критерии приёмки
- [ ] <проверяемое условие>
- [ ] Политика тестов соблюдена (новый устойчивый тест только если он указан выше)
- [ ] Названные страницы `Doc-` актуальны (или «вики не затронута»)

## Связи
- Фича-источник: #NN, [Feature-<Name>](https://github.com/PlutoGr/TravelAgency/wiki/Feature-<Name>)
- Эпик: #NN
- Страница: [Feature-<Name>](https://github.com/PlutoGr/TravelAgency/wiki/Feature-<Name>)
```
