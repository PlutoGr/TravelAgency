# Готово (`статус: готово`)
- **Applies to:** all.
- **Enter:** feature after Приёмка passed and the named `Doc-` page is written; epic after owner acceptance; any item closed as not planned.
- **Close procedure:** the orchestrator closes the issue (`gh issue close <NN>`, with `--reason "not planned"` when cancelled), sets `статус: готово` (removes other status labels), and sets board Status to Готово. Never leave an open issue with `статус: готово`. On reopen, the orchestrator sets the column to restore in the same step. In the same step mark the wiki page (below) and, when a child closes, set its epic's label ([В работе](in-progress.md)).
- **On close:** `.github/workflows/link-closed-by.yml` sets the PR chip. Do not add `closes`/`fix`/`fixes`, do not call `addCloseIssueReferences`, do not add or remove Development links (carve-out: the owner explicitly asks). A follow-up comment may note the merged PR.
- **Wiki:** in the same step as the close, the `Feature-` page gets «сделано» with links (`Doc-`, ADR, PRs); the `Epic-` page gets it only at owner acceptance; cancelled work gets «отменено» with the reason. This is the only moment for these marks.
- **Who moves:** orchestrator.
