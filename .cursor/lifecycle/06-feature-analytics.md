# 06 Feature analytics (column Аналитика)
Product WHY and WHAT for one feature (or bug-feature). No solution design.

**Entry:** feature taken from Очередь in priority order. Set `статус: аналитика` with the take comment; if this is the epic's first child leaving Очередь, set the epic to `статус: в работе` with `Waiting on` = `children` (never for #8). A child of an epic in Удержание is not taken ([Удержание](../elements/board/columns/on-hold.md)).

**Who:** one cloud agent with the [analyst brief](../briefs/analyst.md); the orchestrator reviews and publishes. A gap named at the epic demo still runs this stage: the orchestrator (or analyst) evaluates the owner's proposed fix against alternatives and brings options back; analytics is not skipped ([epic demo](13-epic-acceptance.md)). A thin backlog / #8 child uses this feature route and this column; the orchestrator may write a short feature «Бизнес и сценарии» himself (thin fill), still inside Аналитика.

**Section «Бизнес и сценарии»** on the `Feature-` page ([feature page template](../templates/wiki/feature-page.md)):
1. Inherit the epic's chosen user story. Restate the epic goal only when this feature changes it.
2. Every proposed field, control or block, on a screen or only in the backend: who fills or supplies it, who reads it, why it exists, lifecycle (create, edit, empty, error), how it looks and behaves. A field the service accepts or stores is a product decision. Prefer omit: an item that does not earn its place is left out.
3. Layout when a screen is proposed: grouping, primary action, behavior, use of width and empty space. A pixel mock only when behavior stays unclear without it.
4. Given/When/Then for the behavior this feature must show.
5. Open questions in «Решения» with «Выбрано» empty. A choice only the owner can make: keep `статус: аналитика`, record the `Question` in [memory](../elements/memory.md#question), ask the owner; do not advance until answered.

No services, API paths, storage, migrations or contracts. A bug-feature covers its product change only and adds no unrelated fields.

**Exit gate:** a developer cannot invent a field or block this section did not justify. Every item has who fills, who reads, why, lifecycle, look and behavior; a proposed screen has layout; Given/When/Then present. A name-only field list fails. Then set `статус: архитектура` ([feature Архитектура](07-feature-architecture.md)). [Owner stage approval](../orchestrator.md#owner-stage-approval) applies to epic Бизнес-анализ, epic Архитектура and Разбиение.
