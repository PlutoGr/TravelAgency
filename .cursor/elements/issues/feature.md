# Feature
A demonstrable, checkable result that moves its epic's expectations. Route: [lifecycle](../../lifecycle/README.md#flow). Size at [analytics](../../lifecycle/06-feature-analytics.md#size). Kind and test policy at [split](../../lifecycle/05-epic-split.md#kind-and-test-policy).

- **Created by:** the orchestrator, from a «Разбиение» line or intake. Issue template [feature-issue](../../templates/github/feature-issue.md); page template [feature-page](../../templates/wiki/feature-page.md).
- **Labels:** `enhancement` (or `bug` for a bug-feature) and `статус: очередь` on create. No field «Тип».
- **Page:** `Feature-<Name>` with a thin «Бизнес и сценарии», «Архитектура», «Решения» (when a question is open), «Тест-кейсы» (a case table for Крупная `сначала сценарии`, a short scenario list for Малая with that policy, otherwise one policy line) ([feature pages](../wiki/feature-pages.md)). Header carries `Размер` (written at Аналитика), `Вид` and `Тесты`. Links the epic expectations; does not repeat them.
- **Small work** with its own analysis is a Feature of the same epic. A typo inside an open feature is not a new issue ([bugs](../../lifecycle/bugs.md)).
- **One issue, one open PR, one cloud agent at a time.** A typo found before merge stays in that feature's PR; small related fixes may be batched. A fix found during Приёмка is a second PR on the same issue; one live run per issue ([row lifecycle](../memory.md#row-lifecycle)); the closedBy chip shows one PR and is not a merge gate.
- **One page per open issue:** a `Feature-` page shared by two open issues is split when either is next taken.
- **Close:** after Приёмка passed and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута», by the [close procedure](../board/columns/done.md).
