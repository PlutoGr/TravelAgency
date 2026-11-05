# Feature page
File `Feature-<Name>.md` (also for a bug-feature); sidebar group «Фичи». Rules: [feature pages](../../elements/wiki/feature-pages.md). Short change TZ, a link to the epic expectations, and a link to the epic architecture. The thin TZ is filled at split, or at take when the feature was opened outside a split. «Тест-кейсы» is filled when the route reaches it, or as a policy line at take. No status line. No feature architecture section: a page that still has one is legacy and is dropped on the next edit of that page.

```markdown
# Фича: <название>

Эпик: [Epic-<Name>](Epic-<Name>). Задача: #NN. Размер: <Малая | Крупная>. Вид: <фронт | бэк | документация>. Тесты: <без новых | поправить существующие | сначала сценарии>. <Для баг-фичи: Фича-источник: [Feature-<Name>](Feature-<Name>), #NN.>

Эталон: <мок `mocks/<NN>/index.html`, какие его страницы реализует фича; иначе «нет»>.

Архитектура: [Epic-<Name>](Epic-<Name>)#архитектура.

## Бизнес и сценарии
Ожидание эпика: [Epic-<Name>](Epic-<Name>)#ожидания.

Что меняем и зачем: <коротко. Это не второй бизнес-анализ.>

Соседние фичи: <ссылки или «нет»>.

## Решения
<таблица по шаблону «Решения», когда есть открытый вопрос>

## Тест-кейсы
<Крупная и `сначала сценарии`: таблица ниже. Малая и `сначала сценарии`: короткий список сценариев, без этой таблицы. Иначе одна строка: «без новых» или какие существующие тесты правим.>

| № | Предусловие | Шаги | Ожидаемый результат | Уровень | Автотест |
|---|---|---|---|---|---|
| ТК-1 | ... | ... | ... | unit / integration / e2e / ручная на dev | да / нет |
```
In the same step as the close ([done](../../elements/board/columns/done.md)) add at the top: `Сделано: <ссылки на Doc-, ADR, PR>.`
