# Готово (`статус: готово`)
- **Applies to:** all.
- **Enter:** feature after Приёмка passed and the named `Doc-` page is written; epic after owner acceptance; any item closed as not planned.
- **Close procedure:** close the issue first (`gh issue close <NN>`, with `--reason "not planned"` when cancelled), then add `статус: готово`; on the closed event `board-sync` also sets `статус: готово` and removes the other status labels. Never add `статус: готово` to an open issue: `board-sync` rewrites it to `статус: очередь`. In the same step mark the wiki page (below).
- **On close:** `board-sync` moves the card; `.github/workflows/link-closed-by.yml` sets the PR chip. Do not add `closes`/`fix`/`fixes`, do not call `addCloseIssueReferences`, do not add or remove Development links (carve-out: the owner explicitly asks). A follow-up comment may note the merged PR.
- **Wiki:** in the same step as the close, the `Feature-` page gets «сделано» with links (`Doc-`, ADR, PRs); the `Epic-` page gets it only at owner acceptance; cancelled work gets «отменено» with the reason. This is the only moment for these marks.
- **Who moves:** orchestrator.
