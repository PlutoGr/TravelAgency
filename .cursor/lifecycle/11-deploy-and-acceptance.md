# 11 Deploy and acceptance on dev (column Приёмка)
**Entry:** merged PR and a green «Deploy to dev» for a sha that includes the merge. The «TravelAgency CI/CD» routine wakes the orchestrator ([events](events.md)); then set `статус: приёмка`. No per-task wait-watch.

**Who:** the orchestrator (dev access). Not a cloud launch. The owner may spot-check a major feature; the orchestrator still owns the acceptance comment and the close.

**Steps**
1. Run the feature's manual and e2e cases (for a bug-feature, its cases) plus a short smoke (login, catalog, booking). Check by API or curl first; a browser only for what must be seen. Dev credentials only by file path, never printed.
2. Post one [acceptance comment](../templates/github/acceptance-comment.md) listing cases passed or failed by id.
3. Tiny miss (typo, or a 500 caused by a typo): fix it inside this feature (developer launch, same route through review and merge); stay in Приёмка until the redeploy passes.
4. Substantial miss (add or remove a field, change a screen, change a business rule): open a bug-feature linked to this feature ([bugs](bugs.md)) and take it next. This issue stays open in Приёмка until that bug-feature is Готово and the remaining cases pass here; each issue closes on its own gate.
5. Pass: continue at [12](12-docs-after-merge.md).

**Exit gate:** all cases pass and the named `Doc-` page is written; then close by the [close procedure](../elements/board/columns/done.md).

Later environments: when test and prod exist, a release stage is added here (promotion rules, smoke per environment) as an owner-approved rules change.
