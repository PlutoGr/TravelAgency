# 03 Epic business analysis (column Бизнес-анализ)
Product WHY and WHAT, enough to choose architecture and to split features. Not a screen specification; it does not unlock feature code.

**Entry:** epic in Очередь taken in the agreed order ([02](02-roadmap.md)). Set `статус: бизнес-анализ` with the take comment ([status comments](../templates/github/status-comments.md)).

**Who:** one cloud agent with the [analyst brief](../briefs/analyst.md); the orchestrator reviews and publishes.

**Section «Бизнес-анализ»** on the `Epic-` page ([epic page template](../templates/wiki/epic-page.md)), all blocks visible:
1. Problem now: what hurts, what already works (name the `Doc-` pages), what must not break.
2. Actors: roles and when they care (not tokens or JWT).
3. Named analogues: each comparable product or pattern, what it does, why it fits or does not. "Like other sites" with no name is not evidence.
4. Several solution options on a continuum: more than one, ordered along one axis (for example smaller change to larger change).
5. Chosen user story, and why it is the best fit for us.
6. Non-goals.

No services, API paths, storage, migrations, contracts, field catalog or screen layout. The standing bugs epic uses the same section; it may be short but still says why this class of work exists.

**Exit gate:** all six blocks visible, analogues named, more than one option on a continuum, the chosen story says why it is best, no field catalog and no services. Open questions sit in «Решения». Then set `статус: архитектура` ([04](04-epic-architecture.md)).
