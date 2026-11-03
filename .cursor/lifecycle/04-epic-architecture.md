# 04 Epic architecture (column Архитектура)
**Entry:** Бизнес-анализ passed its gate ([Бизнес-анализ](03-epic-business-analysis.md)). Inputs: fixed «Ожидания» (and «Эталон» when present), the thin `Doc-` skeleton, and code or git when the design needs them.

**Who:** one cloud agent with the [architect brief](../briefs/architect.md); the orchestrator reviews and publishes. The launch names the expectations, the etalon and the touchpoints. It does not paste the methodology.

**Section «Архитектура»** on the `Epic-` page: one block. What changes, where, and how it connects. As-is to to-be at architecture level. No field catalog and no screen layout (the etalon holds the look).
- A pure visual epic stays minimal, often a front-only touch list.
- Do not invent several options for show. Map only what «Ожидания» justified.
- Unrealistic or dangerous: stop. Leave «Выбрано» empty and say why. The orchestrator escalates to the owner. Do not paper over it.
- Unclear expectations: do not rewrite «Ожидания». The epic stays on `статус: архитектура` with a normal owner `Question` ([Question](../elements/memory.md#question)).
- Every important choice is one «Решения» row ([decisions log](../elements/wiki/decisions-log.md)). Fill «Связи с другими эпиками» when known.

**Exit gate:** the one block is visible and a split can follow it; no empty «Выбрано» the split depends on. When that bar holds and no owner `Question` is still open, publish (or leave ready) and ask for [owner stage approval](../orchestrator.md#owner-stage-approval): `Question` = `approve epic architecture #NN`. On yes, clear that `Question` and set `статус: разбиение` ([Разбиение](05-epic-split.md)). On no or edits, stay on `статус: архитектура`, keep that `Question`, rework, and ask again.
