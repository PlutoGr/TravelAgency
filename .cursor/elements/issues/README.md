# Issues
The issue is the control panel: the status label plus the wiki page link. Artifacts live on the wiki, not in chat or in issue threads.

## Types
Two product types. A column is not a type. No subtask, no mini-feature, no third level.

| Type | What | Artifact | Rules |
|---|---|---|---|
| Epic | business goal, including the standing bugs epic | `Epic-<Name>` page + parent issue (`тип: родительская`, field «Тип» = «Эпик») | [epic.md](epic.md) |
| Feature | demonstrable result, including a small feature and a bug-feature | `Feature-<Name>` page + child issue linked to its epic | [feature.md](feature.md), [bug-feature.md](bug-feature.md) |

Non-product issues (process, infra, questions) are not a type of product work: [non-product work](../../lifecycle/non-product-work.md). The roadmap orders epics and is not a task type ([roadmap](../roadmap.md)).

## Common rules
- Created only by the orchestrator, from the template of its kind ([templates](../../templates/README.md#github-artifacts)). Title as a commit title ([commits](../git/commits.md)). Body «Зачем», «Что сделать», «Критерии приёмки» (testable), «Связи».
- On create: exactly `статус: очередь` plus one type label ([labels](labels.md)); «Приоритет» set at once.
- GitHub parent and child (sub-issue) links stay for board control; children of an epic are features or bug-features.
- `<Name>` in page names: Latin, English, Title-Case-With-Hyphens. Do not rename casually.
- Comments: [issue comment template](../../templates/github/issue-comment.md); transitions: [status comments](../../templates/github/status-comments.md). Impersonal Russian. Do not casually rewrite published comments; delete and repost only to remove a forbidden leak (personal name, role label as a person, retired chat-bot name, secret).
- Closed work gets no new documents. Do not retrofit old closed issues.
- The `.github/ISSUE_TEMPLATE/` forms mirror these templates; keep them in sync when a template changes.
