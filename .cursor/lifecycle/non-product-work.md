# Non-product work
Process docs, rules, infra, questions. Not a third type and no fake feature page.

- Issue from the [non-product issue template](../templates/github/non-product-issue.md) with a type label (`documentation`, `question`, or `enhancement` for infra) and exactly one `статус:*`.
- Short column list: Очередь → Разработка (while a PR is open) → Приёмка (only when dev acceptance applies) → Готово. Owner questions: pause in place (same column) + [memory](../elements/memory.md). Never use this shorter list for a product feature or bug-feature.
- The work is still done by a briefed cloud agent when it touches the repo ([devops](../briefs/devops.md) for infra, [methodology auditor](../briefs/methodology-auditor.md) for rules consistency), and still goes through [review and merge](10-review-and-merge.md).
- Changes of meaning in `.cursor/` need the owner's yes before merge ([orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first)).
- Dependabot PRs have no issue ([dependencies](../elements/dependencies.md)). A docs or process PR with no tracker task uses «без задачи» (rare).
- Red CI on `main` or unowned Dependabot red: open or take a [backlog](../project.md#standing-backlog) item and run the product FSM with thin stages ([bugs](bugs.md)); do not invent a shortcut route.
