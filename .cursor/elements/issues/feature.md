# Feature
A demonstrable, checkable result that moves its epic's problem. Route by size: [lifecycle](../../lifecycle/README.md#flow), decided at [analytics](../../lifecycle/06-feature-analytics.md#size).

- **Created by:** the orchestrator, from a «Разбиение» line or intake. Issue template [feature-issue](../../templates/github/feature-issue.md); page template [feature-page](../../templates/wiki/feature-page.md).
- **Labels:** `enhancement` (or `bug` for a bug-feature) and `статус: очередь` on create. No field «Тип».
- **Page:** `Feature-<Name>` with sections «Бизнес и сценарии», «Архитектура», «Решения» (when a question is open), «Тест-кейсы» ([feature pages](../wiki/feature-pages.md)). Links its epic; does not repeat the epic text.
- **Small work** with its own analysis is a Feature of the same epic. A typo inside an open feature is not a new issue ([bugs](../../lifecycle/bugs.md)).
- **One issue, one open PR, one cloud agent at a time.** A typo found before merge stays in that feature's PR; small related fixes may be batched. A fix found during Приёмка is a second PR on the same issue; one live run per issue ([row lifecycle](../memory.md#row-lifecycle)); the closedBy chip shows one PR and is not a merge gate.
- **One page per open issue:** a `Feature-` page shared by two open issues is split when either is next taken.
- **Close:** after Приёмка passed and either the named `Doc-` page is published or the PR «Документация» says «вики не затронута», by the [close procedure](../board/columns/done.md).
