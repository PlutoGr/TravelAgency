# 10 Review and merge (still column Разработка)
Code review is not a board column and is not Демо. Демо is only the epic owner-demo gate ([13](13-epic-acceptance.md)). A fix PR during Приёмка goes through this stage too, and the issue keeps `статус: приёмка`.

**Entry:** the cloud agent reported the PR **ready for orchestrator review**: CI green, Secret scan green, Cursor Automations finished, every bot finding answered or marked, [agent report](../templates/agents/agent-report.md) posted. Until then the agent owns the PR ([cloud agents](../elements/cloud-agents.md), [orchestrator §5.8](../orchestrator.md#58-cloud-agents)).

**Who:** Cursor Automations (first line, while the agent watches), the cloud agent (fixes until ready), the orchestrator (full review and merge), a cloud agent with the [reviewer brief](../briefs/reviewer.md) on escalation.

## 1. Cloud agent until ready
- Watch CI and Automations on the PR; fix red CI that belongs to this PR (flake: one rerun; in-feature failure: fix; expired fixture / upstream digest: fix in this PR if it blocks this work, else raise a question).
- Reply on every finding thread ([review replies](../templates/github/review-replies.md)). «Будет исправлено» is not finished: fix, push, then «исправлено» with the head sha.
- A finding that needs the owner: raise a [question](../templates/agents/question.md); the orchestrator pauses the issue in place (same column) via [memory](../elements/memory.md) until answered: do not advance and do not ask the agent to keep coding past the blocker.
- When all checks are green and findings are finished: post the agent report that the PR is ready. In memory set `Waiting on` = `none` and `Next` = full review.

## 2. Full orchestrator review
- Strip Cursor PR-body markers (`CURSOR_AGENT_PR_BODY`, artifact links) and the «Open in Cursor» footer (`gh pr edit --body-file`).
- Re-read the issue, the agent's report and solution, the whole PR diff, every comment and fix.
- Confirm every bot finding is answered, no critical defect remains, scope matches the page and the epic problem, formats and secrets pass ([PR](../elements/git/pull-requests.md), [security](../rules/security.mdc)).
- Verdict: [deliverable review](../templates/agents/deliverable-review.md). A gap: reply to the same agent (steer) and wait for the new ready report.
- Green CI and «Secret scan» ([CI/CD](../elements/ci-cd.md)). Do not merge while either is red or still running.
- The PR names the `Doc-` page or «вики не затронута»; a missing line blocks merge.
- «Задача» is `#NN` (unless Dependabot or «без задачи»). Do not require a closedBy chip while the issue is open.

## 3. Escalation review
Run a dedicated review (in chat for a trivial diff, otherwise a cloud agent with the [reviewer brief](../briefs/reviewer.md)) when any holds:
- an automation left a blocker or a validated finding the agent could not close;
- the PR is risky (auth, secrets, payments, infra, migrations, public API contracts, runtime or network dependencies);
- the diff is large or crosses services;
- the Feature or Epic page changed in «Архитектура» or «Решения» for this work;
- the orchestrator or the owner asks for it.
Otherwise Automations plus the agent's ready state plus the orchestrator's full review are enough. The verdict is posted as a Russian GitHub review or comment ([review verdict](../templates/github/review-verdict.md)) with the head sha.

## 4. Merge
- Only the orchestrator merges, squash, title per [commits](../elements/git/commits.md) with ` (#NN)`.
- Authorized in-scope work merges without a separate yes; ask first only for the cases in [orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first).
- One PR at a time: the next merge waits until the previous «Deploy to dev» is green.
- Do not squash-merge while a run is in progress, a finding has no reply, or a reply only promises a fix.
- After merge: [merge notice](../templates/owner/merge-notice.md) to the owner; set `Waiting on` = Deploy with the merge sha in [memory](../elements/memory.md); continue at [11](11-deploy-and-acceptance.md). Read `TA: Doc- after merge` ([12](12-docs-after-merge.md)).

**Exit gate:** merged with every gate recorded; the issue stays Разработка (or Приёмка for a fix during acceptance) until Deploy to dev is green.

## Safety-net wakes
A CI/Deploy/Secret-scan wake while an agent owns the PR is expected → sleep after never-stall checks and confirming memory. A wake with no owner, a second branch, or Dependabot → act per [events](events.md).
