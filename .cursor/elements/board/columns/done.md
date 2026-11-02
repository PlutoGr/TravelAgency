# Готово (`статус: готово`)
- **Applies to:** all.
- **Enter:** feature after Приёмка passed and the named `Doc-` page is written; epic after owner acceptance; any item closed as not planned. Set the label and close the issue in the same moment.
- **On close:** `board-sync` moves the card; `.github/workflows/link-closed-by.yml` sets the PR chip. Do not add `closes`/`fix`/`fixes`, do not call `addCloseIssueReferences`, do not add or remove Development links (carve-out: the owner explicitly asks). A follow-up comment may note the merged PR.
- **Wiki:** `Feature-` page marked «сделано» with links; `Epic-` page only after owner acceptance; cancelled work «отменено» with the reason.
- **Who moves:** orchestrator.
