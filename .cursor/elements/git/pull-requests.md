# Pull requests
Template: [pull request](../../templates/github/pull-request.md), mirrored by `.github/pull_request_template.md` (keep both in sync).

- **Who opens:** the cloud agent doing Разработка; a regular PR into `main`. **Who merges:** only the orchestrator ([review and merge](../../lifecycle/10-review-and-merge.md)).
- Title = commit title without the number.
- Sections: «Что сделано», «Зачем», «Как это работает» (simple example first), «Как проверить», «Документация» (`Doc-` page name to write after merge, or «вики не затронута»), «Задача» (`#NN` only, or «без задачи» for a rare docs or process PR with no tracker task; nothing for Dependabot). Tick template checkboxes that hold.
- Never `closes`/`fix`/`fixes`: merge must not auto-close the issue. Do not call `addCloseIssueReferences`, do not attach closedBy before acceptance. The close workflow matches a merged PR by the branch segment and/or `#NN` in the title or body. A missing `#NN` on a tracked task is fixed in the PR text, not with `addCloseIssueReferences`.
- Behaviour, API, contract, settings or infra change: «Документация» names the page; the wiki text is written after merge ([Doc- after merge](../../lifecycle/12-docs-after-merge.md)).
- Russian and impersonal ([templates](../../templates/README.md#russian-and-impersonal)).
- Strip Cursor PR-body markers and the «Open in Cursor» footer before review.
- Review threads: [review replies](../../templates/github/review-replies.md). Escalated verdict: [review verdict](../../templates/github/review-verdict.md).
- Post to the owner only the merge notice, a blocker or a decision question, not progress.
