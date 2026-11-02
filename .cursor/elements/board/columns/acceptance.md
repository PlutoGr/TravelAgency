# Приёмка (`статус: приёмка`)
- **Applies to:** feature, bug-feature; non-product issue when dev acceptance applies.
- **Enter:** after merge and a green Deploy to dev for a sha that includes it ([stage 11](../../../lifecycle/11-deploy-and-acceptance.md)).
- **While here:** the orchestrator runs the cases on dev and posts the [acceptance comment](../../../templates/github/acceptance-comment.md); tiny misses are fixed here (a fix PR keeps `статус: приёмка`); substantial misses become bug-features while this issue stays open; when the PR named a `Doc-` page, its text is finished and published ([stage 12](../../../lifecycle/12-docs-after-merge.md)).
- **Leave:** → Готово when all cases (for a non-product issue, all criteria) pass and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута».
- **Who moves:** orchestrator.
- **Required:** acceptance comment with case ids; a non-product issue uses the criteria pass comment ([acceptance comment](../../../templates/github/acceptance-comment.md)).
