# Разработка (`статус: разработка`)
- **Applies to:** feature, bug-feature; non-product issue while its PR is open.
- **Enter:** product item from Тест-кейсы when «Полное ТЗ» holds ([stage 09](../../../lifecycle/09-development.md)); non-product item from Очередь when taken ([non-product work](../../../lifecycle/non-product-work.md)).
- **While here:** one cloud agent, one PR; review, findings, merge and the wait for Deploy all stay here ([stage 10](../../../lifecycle/10-review-and-merge.md)). Code review is not На ревью.
- **Leave:** → Приёмка after merge and a green Deploy to dev; → На ревью on «нужно решение владельца» (restore Разработка after).
- **Who moves:** orchestrator; the CI/CD wake triggers the move ([events](../../../lifecycle/events.md)).
- **Required:** branch linked, PR with «Задача» `#NN` and «Документация» line, `Waiting on` in the pipeline note after merge.
