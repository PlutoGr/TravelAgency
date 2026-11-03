# 01 Intake and discussion with the owner
Turn an owner ask, a found defect or a roadmap item into a correctly typed, templated issue in Очередь.

**Entry:** the owner writes an idea or request; or a defect is found ([events](events.md)); or the roadmap names the next goal ([epic pick](02-roadmap.md)).

**Who:** the orchestrator, with the owner in Russian.

**Steps**
1. Understand the ask: problem, who benefits, expected outcome, urgency. Ask the owner only what cannot be inferred from the roadmap, `Doc-` pages and open issues. Batch questions in one message ([owner question](../templates/owner/owner-question.md)).
2. Classify:
   - a new business goal → Epic ([epic](../elements/issues/epic.md));
   - a demonstrable result inside an existing epic → Feature under that epic ([feature](../elements/issues/feature.md));
   - a defect → [bugs.md](bugs.md) (tiny fix inside the open feature, bug-feature of the current epic, or the [standing backlog](../project.md#standing-backlog));
   - process docs, infra, a question → [non-product-work.md](non-product-work.md).
3. Check duplicates and the roadmap. If the ask changes the roadmap or product scope, confirm with the owner and update the sketch ([roadmap](02-roadmap.md)).
4. Create the issue from its template (`статус: очередь` only) and, for an epic or feature, the wiki page from its template ([epic page](../templates/wiki/epic-page.md), [feature page](../templates/wiki/feature-page.md)) with a sidebar entry. An epic the owner asked to open («заведём эпик» plus a free-form brief) gets the brief structured as initial wishes: issue «Зачем» and page «Исходные пожелания». Those wishes are changeable and are not the exit truth; «Ожидания» are fixed later in [Бизнес-анализ](03-epic-business-analysis.md). Until the epic is opened on the roadmap it is one sketch line; once opened, the epic appears there. **Same turn:** add the Project card if it is missing and set board Status = Очередь. Project Status has no default ([board](../elements/board/README.md)).
5. Set «Приоритет» at once ([queue priority](../elements/board/queue-priority.md)), except a child of an epic in Удержание, which stays unnumbered.
6. Confirm to the owner in one line with the issue link, only if the owner asked for it.

**Foreign-filed issues:** anyone may file via `.github/ISSUE_TEMPLATE/`. On the next owner message or weekly audit, bring each new foreign issue to its template, exactly one `статус:*`, Status = Очередь, and «Приоритет».

**Output:** issue in Очередь with the template body, page link in «Связи» (epic/feature), priority set, card on the board with Status = Очередь.

**Exit gate:** the type is right, the body is from the template, exactly one `статус:*`, Status = Очередь, «Приоритет» set. An ask the owner has not put in work stays in Очередь until the owner or the roadmap orders it.
