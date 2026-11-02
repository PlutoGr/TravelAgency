# Feature
A demonstrable, checkable result that moves its epic's problem. Route: [lifecycle stages 06-12](../../lifecycle/README.md#stages).

- **Created by:** the orchestrator, from a «Разбиение» line or intake. Issue template [feature-issue](../../templates/github/feature-issue.md); page template [feature-page](../../templates/wiki/feature-page.md).
- **Labels:** `enhancement` (or `bug` for a bug-feature) and `статус: очередь` on create. No field «Тип».
- **Page:** `Feature-<Name>` with sections «Аналитика», «Архитектура», «Решения» (when a question is open), «Тест-кейсы» ([feature pages](../wiki/feature-pages.md)). Links its epic; does not repeat the epic text.
- **Small work** with its own analysis is a Feature of the same epic. A typo inside an open feature is not a new issue ([bugs](../../lifecycle/bugs.md)).
- **One issue = one owner = one PR** (small related fixes may be batched).
- **Close:** after Приёмка passed and the named `Doc-` page is written, together with `статус: готово` ([done](../board/columns/done.md)).
