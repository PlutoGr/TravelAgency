# Демо (`статус: демо`)
**Only for epic owner demo** after every listed child is Готово, before Готово ([stage 13](../../../lifecycle/13-epic-acceptance.md)). Not a code-review column. Not a parking place for owner questions. Features never enter this column (a feature exits via Приёмка → Готово).

- **Applies to:** product epic after every listed child is Готово. Never the standing backlog (#8). Never a feature or bug-feature.
- **Enter:** with the [epic demo](../../../templates/owner/epic-demo.md). Record in [memory](../../memory.md): `Waiting on` = `owner`, `Next` = record accept or gaps, Pause/why = epic demo (so memory-refresh and the Monday audit re-ask it). Same turn as the last child close when no siblings remain open ([done](done.md)).
- **While here:** the owner accepts or names gaps; in-flight non-roadmap work (backlog, non-product, Dependabot) may continue; the next roadmap epic waits until this epic is Готово.
- **Leave:** → Готово on owner accept (then epic pick); → В работе on gaps (new Очередь children via Аналитика); → В работе in the same step when a child is reopened ([done](done.md)).
- **Other owner questions:** the demo row's owner slot holds only the demo ask; never put a second question there. A question about other work goes on that work's own issue and memory row; a new question about this epic waits until the demo verdict (gaps then go through Аналитика as new children, which ask on their own issues). Silence policy only ([orchestrator §8](../../../orchestrator.md#8-never-stall)). Never park elsewhere for a question.
- **Who moves:** orchestrator sets `статус: демо` and board Status = Демо in the same step.
