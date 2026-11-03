# 06 Feature analytics (column Аналитика)
A thin confirm that this feature executes the epic plan. Not a second business analysis, not a field catalog, and not an owner stage-approval gate.

**Entry:** feature taken from Очередь in priority order. Set `статус: аналитика` with the take comment; if this is the epic's first child leaving Очередь, set the epic to `статус: в работе` with `Waiting on` = `children` (never for #8). A child of an epic in Удержание is not taken ([Удержание](../elements/board/columns/on-hold.md)).

**Who:** the orchestrator writes the section. A cloud analyst is not the default. Launch one only for a narrow helper (one open decision, a short UX note on a screen) with a short brief ([analyst](../briefs/analyst.md)). A gap from the epic demo still enters this column as the same thin confirm; the owner's words are the input, not a prompt for an options essay ([epic demo](13-epic-acceptance.md)). A contradiction with the fixed «Ожидания» is an owner question, pause in place.

**Section «Бизнес и сценарии»** on the `Feature-` page ([feature page template](../templates/wiki/feature-page.md)):
1. Link the epic «Ожидания» (a legacy «Бизнес и сценарии» on the epic is that section until the page is refreshed).
2. What this feature changes and why that serves the exit result. One short block. Sibling features by link.
3. Echo `Вид` and `Тесты` from the split ([kind and test policy](05-epic-split.md#kind-and-test-policy)). When this feature was opened outside a split, write them in this column. Correct `Тесты` when this slice is an important new API and the recorded value said otherwise; record why in «Решения».
4. Open questions in «Решения» with «Выбрано» empty. A choice only the owner can make: keep `статус: аналитика`, record a normal `Question`, and do not advance until answered. Do not set `approve feature BA #NN`.

No services, API paths, storage, migrations or contracts. A bug-feature covers its product change only.

## Size
Recorded at this exit, on the issue and on the Feature page, as `Размер: Малая` or `Размер: Крупная`. Use only the visible part of [review §3](10-review-and-merge.md#3-escalation-review): Крупная when that section already calls the work Крупная, otherwise Малая. Do not copy the list. Writing «Архитектура» for this feature is not a trigger. An item already past this exit is not sized and keeps the route it is already on ([in-flight](README.md#gates)).

Малая then uses one architecture step (the test-policy line, or a short scenario list when `Тесты` is `сначала сценарии`) and a `Doc-` draft in the PR only when the skeleton needs it, published right after merge. Крупная with `Тесты: сначала сценарии` keeps a separate Тест-кейсы column. Any other Крупная passes Тест-кейсы in the architecture move. A later escalation on a Малая head does not change `Размер` or this route ([review §3](10-review-and-merge.md#3-escalation-review)). Both reach Готово only when every named `Doc-` page is current, or the PR says «вики не затронута».

**Exit gate:** the thin section is visible, `Размер`, `Вид` and `Тесты` are on the issue and the page, and no owner-only choice is still open. The orchestrator publishes and sets `статус: архитектура` ([feature Архитектура](07-feature-architecture.md)). There is no owner stage approval on this column. A row that already has `Question` = `approve feature BA #NN` is legacy: it still waits for the owner's yes ([Question](../elements/memory.md#question)).
