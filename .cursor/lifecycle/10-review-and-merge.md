# 10 Review and merge (still column Разработка)
Code review is not column На ревью. На ревью is only an owner decision.

**Entry:** the cloud agent reported a ready PR ([09](09-development.md)).

**Who:** Cursor Automations (first line), the orchestrator (gate and merge), a cloud agent with the [reviewer brief](../briefs/reviewer.md) on escalation.

## 1. Accept the agent's result
- Strip Cursor PR-body markers (`CURSOR_AGENT_PR_BODY`, artifact links) and the «Open in Cursor» footer (`gh pr edit --body-file`).
- Check the diff against the page, the stage bar and the issue scope (no drive-by), formats ([PR](../elements/git/pull-requests.md), [commits](../elements/git/commits.md)), secrets ([security](../rules/security.mdc)), and the agent's open doubts. Verdict: [deliverable review](../templates/agents/deliverable-review.md). A gap: reply to the same agent and wait for the new head.

## 2. First line, every PR
- Wait until CI and Cursor Automations have finished; an in-progress run is not the review ([automations](../elements/automations.md)).
- Read every finding (PR review, Find vulnerabilities, Bugbot, CI triage if CI was red), including medium and lower.
- Green CI ([CI/CD](../elements/ci-cd.md)). Red CI: ask the owner before merge.
- The PR names the `Doc-` page or «вики не затронута»; a missing line blocks merge.
- The branch is linked or the close workflow can match the PR, and «Задача» is `#NN` (unless Dependabot or «без задачи»). Do not require a closedBy chip while the issue is open; do not call `addCloseIssueReferences`.

## 3. Findings: reply before merge
Every open finding gets a reply on its thread ([review replies](../templates/github/review-replies.md)). «Будет исправлено» is not finished: fix, push, then reply «исправлено» with the head sha. «Нужно решение владельца» blocks merge: `статус: на ревью`, the comment names Разработка to restore. Leave each thread clearly finished and resolved when the UI allows. Do not re-do a checklist the automations already covered.

## 4. Escalation review
Run a dedicated review (in chat for a trivial diff, otherwise a cloud agent with the [reviewer brief](../briefs/reviewer.md)) when any holds:
- an automation left a blocker or a validated finding;
- the PR is risky (auth, secrets, payments, infra, migrations, public API contracts, runtime or network dependencies);
- the diff is large or crosses services;
- the Feature or Epic page changed in «Архитектура» or «Решения» for this work;
- the orchestrator or the owner asks for it.
Otherwise automations plus green CI are enough; do not require this pass on every routine PR. The verdict is posted as a Russian GitHub review or comment ([review verdict](../templates/github/review-verdict.md)) with the head sha. Page mismatch: changes requested, or a «Решения» row if the page is wrong. Server-side security for risky infra still needs the orchestrator's server check.

## 5. Merge
- Only the orchestrator merges, squash, title per [commits](../elements/git/commits.md) with ` (#NN)`.
- Authorized in-scope work merges without a separate yes; ask first only for the cases in [orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first).
- One PR at a time: the next merge waits until the previous «Deploy to dev» is green.
- Do not squash-merge while a run is in progress, a finding has no reply, or a reply only promises a fix.
- After merge: [merge notice](../templates/owner/merge-notice.md) to the owner; set `Waiting on` = Deploy in the [pipeline note](../elements/pipeline-note.md); continue at [11](11-deploy-and-acceptance.md). Read `TA: Doc- after merge` ([12](12-docs-after-merge.md)).

**Exit gate:** merged with every gate recorded; the issue stays Разработка until Deploy to dev is green.
