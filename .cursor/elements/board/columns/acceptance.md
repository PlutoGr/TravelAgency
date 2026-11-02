# Приёмка (`статус: приёмка`)
- **Applies to:** feature, bug-feature; non-product issue when dev acceptance applies.
- **Enter:** after merge and a green Deploy to dev whose deployed sha includes it ([Приёмка](../../../lifecycle/11-deploy-and-acceptance.md)).
- **While here:** the orchestrator runs the cases on dev and posts the [acceptance comment](../../../templates/github/acceptance-comment.md); tiny misses are fixed here (a fix PR keeps `статус: приёмка`); substantial misses become bug-features while this issue stays open; when the PR named a `Doc-` page, that page is current before leave ([Doc- after merge](../../../lifecycle/12-docs-after-merge.md)): Малая from the PR draft, Крупная from the after-merge draft.
- **Leave:** → Готово when all cases (for a non-product issue, all criteria) pass and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута»; → Разработка when a green Deploy (a rollback) no longer contains the row's match sha ([row lifecycle](../../memory.md#row-lifecycle)).
- **Who moves:** orchestrator.
- **Required:** acceptance comment with case ids; a non-product issue uses the criteria pass comment ([acceptance comment](../../../templates/github/acceptance-comment.md)).
