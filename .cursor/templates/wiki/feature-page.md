# Feature page
File `Feature-<Name>.md` (also for a bug-feature); sidebar group «Фичи». Rules: [feature pages](../../elements/wiki/feature-pages.md). Short change TZ plus a link to the epic expectations. Fill the current column's section plus «Решения» rows when a question is open; no status line.

```markdown
# Фича: <название>

Эпик: [Epic-<Name>](Epic-<Name>). Задача: #NN. Размер: <Малая | Крупная>. Вид: <фронт | бэк | документация>. Тесты: <без новых | поправить существующие | сначала сценарии>. <Для баг-фичи: Фича-источник: [Feature-<Name>](Feature-<Name>), #NN.>

## Бизнес и сценарии
Ожидание эпика: [Epic-<Name>](Epic-<Name>)#ожидания.

Что меняем и зачем: <коротко. Это не второй бизнес-анализ.>

Соседние фичи: <ссылки или «нет»>.

## Архитектура
<Нетривиальный бэк: модель, API, контракты, миграции, влияние. Фронт или документация: отсылка к архитектуре эпика и к эталону, список касаний.>

## Решения
<таблица по шаблону «Решения», когда есть открытый вопрос>

## Тест-кейсы
<`сначала сценарии`: таблица ниже. Иначе одна строка: «без новых» или какие существующие тесты правим.>

| № | Предусловие | Шаги | Ожидаемый результат | Уровень | Автотест |
|---|---|---|---|---|---|
| ТК-1 | ... | ... | ... | unit / integration / e2e / ручная на dev | да / нет |
```
In the same step as the close ([done](../../elements/board/columns/done.md)) add at the top: `Сделано: <ссылки на Doc-, ADR, PR>.`
