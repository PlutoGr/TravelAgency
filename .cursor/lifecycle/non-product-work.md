# Non-product work
Process docs, rules, infra, questions, urgent fixes. Not a third type and no fake feature page.

- Issue from the [non-product issue template](../templates/github/non-product-issue.md) with a type label (`documentation`, `question`, or `enhancement` for infra and urgent fixes) and exactly one `статус:*`.
- Short column list: Очередь → Разработка (while a PR is open) → Приёмка (only when dev acceptance applies) → Готово; На ревью when the owner must decide. Never use this shorter list for a product feature or bug-feature.
- The work is still done by a briefed cloud agent when it touches the repo ([devops](../briefs/devops.md) for infra, [methodology auditor](../briefs/methodology-auditor.md) for rules consistency), and still goes through [review and merge](10-review-and-merge.md).
- Changes of meaning in `.cursor/` need the owner's yes before merge ([orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first)).

## Urgent fix
Red CI or a failed Deploy on `main` with a code or fixture cause, an expired fixture or upstream digest, or a validated security vulnerability whose fix does not change a business rule: a non-product issue (label `enhancement`, title type `исправление`), taken at once with the short column list, one fix PR with a regression test. A vulnerability fix that changes a business rule is a bug-feature on the full route.

## Other
- Dependabot PRs have no issue ([dependencies](../elements/dependencies.md)). A docs or process PR with no tracker task uses «без задачи» (rare).
