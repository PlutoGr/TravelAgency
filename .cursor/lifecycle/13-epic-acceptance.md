# 13 Epic completion and owner demo (epic В работе → На ревью → Готово)
**Entry:** every child of the epic is Готово (closed). Do not close the epic on child closes alone.

**Who:** the orchestrator prepares the demo; the owner accepts.

**Steps**
1. STOP taking new roadmap work. Set the epic `статус: на ревью`.
2. Send the owner the [epic demo](../templates/owner/epic-demo.md): what shipped (features, `Doc-` and wiki links), how to check on dev, and the checklist: the problem, the chosen user story and the non-goals from «Бизнес и сценарии». Closed children are not enough.
3. Owner pass: close the epic by the [close procedure](../elements/board/columns/done.md) and mark the `Epic-` page «сделано» in the same step (a content mark, not a status). Then epic pick ([02](02-roadmap.md)).
4. Owner finds gaps: set the epic back to `статус: в работе`. Related gaps become features or bug-features under this epic, in Очередь with priority. Unrelated discoveries go to the standing bugs epic. Repeat this stage after they are done.

The standing bugs epic uses the same columns but never blocks on this stage: it waits in Очередь when it has no open children and reaches На ревью only when the owner asks to close it ([В работе](../elements/board/columns/in-progress.md)).

**Exit gate:** recorded owner acceptance («**Итог:** решено: ...» on the epic issue).
