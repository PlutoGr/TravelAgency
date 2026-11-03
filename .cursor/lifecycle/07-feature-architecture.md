# 07 Feature architecture (column Архитектура)
**Entry:** Аналитика passed its gate, including the recorded size, `Вид` and `Тесты` ([Аналитика](06-feature-analytics.md)). Refuse the column when size or test policy is missing, except an in-flight item that keeps the route it is already on ([in-flight](README.md#gates)).

**Who:** one cloud agent with the [architect brief](../briefs/architect.md) when the slice is non-trivial backend (model, API, contracts, migrations). Pure front or visual: the orchestrator writes a thin note that points at the epic architecture and the etalon; a cloud agent only when the touch list is unclear. `Вид: документация`: a thin note by the orchestrator. The orchestrator reviews and publishes any cloud text.

**Section «Архитектура»** on the `Feature-` page, only for what the epic expectations and the thin TZ justify:
- Non-trivial back: model, API (method, path, access, body, response, errors), contracts, migrations, impact.
- Front or docs: a short touch list and a pointer to the epic block and the etalon. Do not redesign the look.
- A slice that needs its own product decision is another Feature under the same epic.
- Unclear expectations: do not rewrite them; options go to «Решения». An impactful «how» not already authorized goes to the owner via the orchestrator. Once the row has «Выбрано», or the owner already put this task in work, a matching PR needs no second yes at merge.
- Name the parent epic's architecture choices this feature relies on.
- In this same step, when this feature will not enter [Тест-кейсы](08-test-cases.md): write «Тест-кейсы» as the policy line (`без новых` or which existing tests change). Малая with `Тесты: сначала сценарии` also gets a short scenario list here, not a second launch.

**Exit gate:** the note is visible and consistent with the epic architecture; no row for this slice has an empty «Выбрано». Крупная with `Тесты: сначала сценарии`: set `статус: тест-кейсы`. Every other feature: «Тест-кейсы» is already the policy line or the short scenario list; one move sets `статус: разработка` ([Разработка](09-development.md)). The stage comment names Разработка and states that Тест-кейсы passed in this same move. The issue does not take `статус: тест-кейсы`. This column is not an [owner stage approval](../orchestrator.md#owner-stage-approval) column.
