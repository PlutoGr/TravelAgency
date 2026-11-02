# Приёмка (`статус: приёмка`)
- **Applies to:** feature, bug-feature; non-product issue when dev acceptance applies.
- **Enter:** after merge and a green Deploy to dev for a sha that includes it ([stage 11](../../../lifecycle/11-deploy-and-acceptance.md)).
- **While here:** the orchestrator runs the cases on dev and posts the [acceptance comment](../../../templates/github/acceptance-comment.md); tiny misses are fixed here; substantial misses become bug-features while this issue stays open; the named `Doc-` page is written ([stage 12](../../../lifecycle/12-docs-after-merge.md)).
- **Leave:** → Готово when all cases pass and the named `Doc-` page is written.
- **Who moves:** orchestrator.
- **Required:** acceptance comment with case ids.
