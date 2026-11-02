# Non-product work
Process docs, rules, infra, questions. Not a third type and no fake feature page.

- Issue from the [non-product issue template](../templates/github/non-product-issue.md) with a type label (`documentation`, `question`, or `enhancement` for infra) and exactly one `статус:*`.
- Short column list: Очередь → Разработка (while a PR is open) → Приёмка (only when the issue criteria require a dev check) → Готово. Owner questions: pause in place (same column) + [memory](../elements/memory.md). Never use this shorter list for a product feature or bug-feature.
- **Exit without a dev check:** after merge, Deploy does not change the column (it still gates the next merge). When the criteria pass and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута», post the [criteria pass comment](../templates/github/acceptance-comment.md) and close from Разработка by the [close procedure](../elements/board/columns/done.md) (label and Status Готово, then close) in the same turn.
- **Exit with a dev check:** green Deploy → Приёмка ([11](11-deploy-and-acceptance.md)); the criteria pass comment replaces feature case ids; then the same close.
- **`question` issue with no PR:** stays in its column until the answer is recorded («**Итог:** решено: ...» on the issue), then closes the same way (with no PR there is no `Doc-` page to wait for).
- The work is still done by a briefed cloud agent when it touches the repo ([devops](../briefs/devops.md) for infra, [methodology auditor](../briefs/methodology-auditor.md) for rules consistency), and still goes through [review and merge](10-review-and-merge.md).
- Changes of meaning in `.cursor/` need the owner's yes before merge ([orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first)).
- Dependabot PRs have no issue ([dependencies](../elements/dependencies.md)). A docs or process PR with no tracker task uses «без задачи» (rare).
- Red CI on `main` or unowned Dependabot red: open or take a [backlog](../project.md#standing-backlog) item and run the product FSM with thin stages ([bugs](bugs.md)); do not invent a shortcut route.
