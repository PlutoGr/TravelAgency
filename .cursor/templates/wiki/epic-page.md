# Epic page
File `Epic-<Name>.md`; sidebar group «Фичи». Rules: [epic pages](../../elements/wiki/epic-pages.md). Three working blocks, then the feature list: initial wishes, fixed expectations, architecture. Fill the current column's section plus «Решения» rows when a question is open; no status line. A legacy page may still title the expectations «Бизнес и сценарии» until it is next edited.

```markdown
# Эпик: <название>

Задача: #NN.

## Исходные пожелания
<что владелец сформулировал на входе; меняемо, не итог>

## Связи с другими эпиками
- Зависит от: [Epic-<Name>](Epic-<Name>) или «нет».
- Параллельно с: [Epic-<Name>](Epic-<Name>) или «нет».
- Почему: <один абзац>.

## Ожидания
### Результат на выходе
<что должно получиться к концу эпика>
### Сейчас, позже, заглушка, отказ
<что реально сейчас, что позже, что заглушка, от чего отказываемся>
### Не делаем
- ...
### Эталон
<если результат меняет экраны: утверждённый кликабельный мок `mocks/<NN>/index.html` в репозитории вики и какие страницы он покрывает; иначе «нет»>

<Проблема и прочий контекст — только если без них следующие стадии не читаются.>

## Архитектура
<один блок: что менять, где, как стыкуется; as-is → to-be. Для чистого визуала — короткий список касаний фронта.>

## Решения
<таблица по шаблону «Решения», когда есть открытый вопрос>

## Разбиение
| Фича | Вид | Зачем | Тесты | Страница | Задача |
|---|---|---|---|---|---|
| <название> | фронт / бэк / документация | <одна строка> | без новых / поправить существующие / сначала сценарии | [Feature-<Name>](Feature-<Name>) | #NN |
```
In the same step as the close after owner acceptance ([done](../../elements/board/columns/done.md)) add at the top: `Сделано: <ссылки на Doc-, PR>.`
