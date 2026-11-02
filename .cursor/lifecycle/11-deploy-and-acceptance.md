# 11 Deploy and acceptance on dev (column Приёмка)
**Entry:** merged PR and a green «Deploy to dev» for a sha that includes the merge; applies to a feature, a bug-feature, or a non-product issue whose criteria need a dev check (other non-product issues close from Разработка, [non-product work](non-product-work.md)). The «TravelAgency CI/CD» routine wakes the orchestrator ([events](events.md)); then set `статус: приёмка`. No per-task wait-watch.

**Who:** the orchestrator (dev access). Not a cloud launch. The owner may spot-check a major feature; the orchestrator still owns the acceptance comment and the close.

**Steps**
1. Run the feature's manual and e2e cases (for a bug-feature, its cases) plus a short smoke (login, catalog, booking). Check by API or curl first; a browser only for what must be seen. Dev credentials only by file path, never printed.
2. Post one [acceptance comment](../templates/github/acceptance-comment.md) listing cases passed or failed by id.
3. Tiny miss (typo, or a 500 caused by a typo): fix it inside this feature (developer launch, same route through review and merge); stay in Приёмка until the redeploy passes. Keep memory `Next` and `Waiting on` current.
4. Substantial miss (add or remove a field, change a screen, change a business rule): open a bug-feature linked to this feature ([bugs](bugs.md)) and take it next. This issue stays open in Приёмка until that bug-feature is Готово and the remaining cases pass here; each issue closes on its own gate. Memory: `Next` = wait until #NN is Готово, `Waiting on` = `none`.
5. Pass: when the PR named a `Doc-` page, finish and publish it ([12](12-docs-after-merge.md)); its draft may already exist from the merge. When the PR says «вики не затронута», close at once.

**Exit gate:** all cases pass and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута»; then close by the [close procedure](../elements/board/columns/done.md) in the same turn.

Later environments: when test and prod exist, a release stage is added here (promotion rules, smoke per environment) as an owner-approved rules change.
