# 13 Epic completion and owner demo (epic В работе → На ревью → Готово)
**Entry:** every issue number listed in the epic's «Разбиение», and every other open child, is Готово (closed). An empty child list is not a finished epic. Do not close the epic on child closes alone.

**Who:** the orchestrator prepares the demo; the owner accepts.

**Steps**
1. STOP taking new roadmap work. Set the epic `статус: на ревью`.
2. Send the owner the [epic demo](../templates/owner/epic-demo.md): what shipped (features, `Doc-` and wiki links), how to check on dev, and the checklist: the problem, the chosen user story and the non-goals from «Бизнес и сценарии». Closed children are not enough.
3. Owner pass: close the epic by the [close procedure](../elements/board/columns/done.md) and mark the `Epic-` page «сделано» in the same step (a content mark, not a status). Then epic pick ([02](02-roadmap.md)).
4. Owner finds gaps: set the epic back to `статус: в работе`. Related gaps become features or bug-features under this epic, in Очередь with priority. The owner's wording is input to Аналитика, not a skip: the new feature still enters Аналитика; evaluate the proposed fix versus alternatives and bring options if needed. Unrelated discoveries go to the [standing backlog](../project.md#standing-backlog). Repeat this stage after they are done.

**Interim demo:** the owner may ask for a demo before every child is Готово. Send the [epic demo](../templates/owner/epic-demo.md) marked interim; the epic keeps its column and is not closed; record what the owner decided on the epic issue.

The standing backlog (#8) is not closed by child Готово; it stays open as the catch-all ([В работе](../elements/board/columns/in-progress.md) does not apply to it as a product epic).

**Exit gate:** recorded owner acceptance («**Итог:** решено: ...» on the epic issue).
