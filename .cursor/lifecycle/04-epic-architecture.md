# 04 Epic architecture (column Архитектура)
**Entry:** Бизнес-анализ passed its gate ([Бизнес-анализ](03-epic-business-analysis.md)).

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews and publishes.

**Section «Архитектура»** on the `Epic-` page: services, boundaries, integration options and why the option was chosen. No field catalog and no screen layout.
- Map only what Бизнес-анализ justified. Add no field, screen or block it omitted. Do not redesign product layout.
- Business part unclear: do not rewrite it; options go to «Решения».
- Every important choice is one «Решения» row ([decisions log](../elements/wiki/decisions-log.md)). An impactful «how» that is not already authorized goes to the owner via the orchestrator.
- Fill «Связи с другими эпиками» (depends on, parallel with, why) when known.

**Exit gate:** architecture text visible, every choice logged, no empty «Выбрано» the split depends on ([decisions log](../elements/wiki/decisions-log.md)). Then set `статус: разбиение` ([Разбиение](05-epic-split.md)).
