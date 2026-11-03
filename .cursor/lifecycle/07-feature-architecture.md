# 07 Feature architecture (column Архитектура)
**Entry:** Аналитика passed its gate, including the recorded size ([Аналитика](06-feature-analytics.md)). The architect refuses the column when size is missing, except an in-flight item that keeps the full route without a size ([in-flight](README.md#gates)).

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews and publishes. On Малая the same run also writes «Тест-кейсы» to the [test-case bar](08-test-cases.md). A second test-designer launch is only for Крупная.

**Section «Архитектура»** on the `Feature-` page: model, API (method, path, access, body, response, errors), contracts, migrations, impact on existing code, only for items Аналитика justified.
- Add no field, screen or block Аналитика omitted; do not redesign layout.
- A slice that needs its own product analysis is another Feature under the same epic.
- Business part unclear: do not rewrite it; options go to «Решения». Every important choice is one row. An impactful «how» not already authorized goes to the owner via the orchestrator. Once the row has «Выбрано», or the owner already put this task in work, a matching PR needs no second yes at merge.
- Name the parent epic's architecture choices this feature relies on.

**Exit gate:** architecture visible and consistent with the epic architecture; every choice logged; no row for this slice with an empty «Выбрано». Крупная: then set `статус: тест-кейсы` ([Тест-кейсы](08-test-cases.md)). Малая: «Тест-кейсы» is visible in the same step and meets the test-case bar; one move sets `статус: разработка` ([Разработка](09-development.md)). The stage comment names Разработка as the next column and states that Тест-кейсы passed in this same move. The issue does not take `статус: тест-кейсы`. [Owner stage approval](../orchestrator.md#owner-stage-approval) applies to epic Бизнес-анализ, epic Архитектура, Разбиение and feature and bug-feature Аналитика, not to feature Архитектура.
