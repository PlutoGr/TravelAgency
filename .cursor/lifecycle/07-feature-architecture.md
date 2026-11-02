# 07 Feature architecture (column Архитектура)
**Entry:** Аналитика passed its gate ([06](06-feature-analytics.md)). The architect refuses the column otherwise.

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews and publishes.

**Section «Архитектура»** on the `Feature-` page: model, API (method, path, access, body, response, errors), contracts, migrations, impact on existing code, only for items Аналитика justified.
- Add no field, screen or block Аналитика omitted; do not redesign layout.
- A slice that needs its own product analysis is another Feature under the same epic.
- Business part unclear: do not rewrite it; options go to «Решения». Every important choice is one row. An impactful «how» not already authorized goes to the owner via the orchestrator. Once the row has «Выбрано», or the owner already put this task in work, a matching PR needs no second yes at merge.
- Name the parent epic's architecture choices this feature relies on.

**Exit gate:** architecture visible and consistent with the epic architecture; every choice logged; no row for this slice with an empty «Выбрано». Then set `статус: тест-кейсы` ([08](08-test-cases.md)).
