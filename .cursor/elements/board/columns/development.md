# Разработка (`статус: разработка`)
- **Applies to:** feature, bug-feature; non-product issue while its PR is open.
- **Enter:** product item from Тест-кейсы when «Полное ТЗ» holds ([stage 09](../../../lifecycle/09-development.md)); non-product item from Очередь when taken ([non-product work](../../../lifecycle/non-product-work.md)).
- **While here:** one cloud agent, one PR; review, findings, merge and the wait for Deploy all stay here ([stage 10](../../../lifecycle/10-review-and-merge.md)). Code review is not a board column and is not Демо.
- **Leave:** → Приёмка after merge and a green Deploy to dev, for a feature, a bug-feature, or a non-product issue whose criteria need a dev check. A non-product issue whose criteria need no dev check: Deploy does not change the column; when its criteria pass and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута», → Готово from here by the [close procedure](done.md). The green Deploy still gates the next merge. Owner question: stay here, pause in [memory](../../memory.md).
- **Who moves:** orchestrator (label + Status). Deploy readiness may arrive via safety-net wake ([events](../../../lifecycle/events.md)).
- **Required:** branch linked, PR with «Задача» `#NN` and «Документация» line, wait row in [memory](../../memory.md) after merge.
