# 10 Review and merge (still column Разработка)
Code review is not a board column and is not Демо. Демо is only the epic owner-demo gate ([13](13-epic-acceptance.md)). A fix PR during Приёмка goes through this stage too, and the issue keeps `статус: приёмка`.

## Ready predicate
A PR is **ready** only when all four hold for its head sha: «CI» green, «Secret scan» green, Cursor Automations finished, every finding answered or marked (a reply that only promises a fix does not count). This is the one definition; every other file links here.

**Entry:** the cloud agent reported the PR ready ([ready predicate](#ready-predicate)) and posted the [agent report](../templates/agents/agent-report.md). Until then the agent owns the PR ([cloud agents](../elements/cloud-agents.md), [orchestrator §5.8](../orchestrator.md#58-cloud-agents)).

**Who:** Cursor Automations (first line, while the agent watches), the cloud agent (fixes until ready), the orchestrator (full review and merge), a cloud agent with the [reviewer brief](../briefs/reviewer.md) on escalation.

## 1. Cloud agent until ready
- Watch CI and Automations on the PR; fix red CI that belongs to this PR (flake: one rerun; in-feature failure: fix; expired fixture / upstream digest: fix in this PR if it blocks this work, else raise a question).
- Reply on every finding thread ([review replies](../templates/github/review-replies.md)). «Будет исправлено» is not finished: fix, push, then «исправлено» with the head sha.
- A finding that needs the owner: raise a [question](../templates/agents/question.md) and end the run with `RESULT blocked`; the orchestrator pauses the issue in place (same column) via [memory](../elements/memory.md) until answered: do not advance and do not ask the agent to keep coding past the blocker.
- When the [ready predicate](#ready-predicate) holds: post the agent report that the PR is ready and finish the run.

On that finish wake the orchestrator clears `Agent` in memory, writes `Waiting on` = `none`, `Next` = full review, and starts the full review in the same wake.

## 2. Full orchestrator review
- Strip Cursor PR-body markers (`CURSOR_AGENT_PR_BODY`, artifact links) and the «Open in Cursor» footer (`gh pr edit --body-file`).
- Re-read the issue, the agent's report and solution, the whole PR diff, every comment and fix.
- Confirm every bot finding is answered, no critical defect remains, scope matches the page and the epic problem, formats and secrets pass ([PR](../elements/git/pull-requests.md), [security](../rules/security.mdc)).
- Verdict: [deliverable review](../templates/agents/deliverable-review.md). A gap: reply to the same agent (steer) and, in the same turn, set `Agent` to the new run, `Waiting on` = `agent` (the agent is the wait here; [cloud agents](../elements/cloud-agents.md#acceptance)), `Next` = wait for the new ready report. Back to `none` and full review only on that finish.
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
- One PR at a time: the next merge waits until the previous «Deploy to dev» is green. Exception: the one repair PR while the queue is stopped ([below](#repair-while-the-queue-is-stopped)).
- Do not squash-merge while a run is in progress, a finding has no reply, or a reply only promises a fix.
- After merge: [merge notice](../templates/owner/merge-notice.md) to the owner; set `Waiting on` = `Deploy` with `entry=<merge sha> fix=-` in [memory](../elements/memory.md#sha-cell) (a fix PR during Приёмка keeps `entry`, sets `fix` to its merge and drops its `head`). On green Deploy: a feature, a bug-feature, or a non-product issue whose criteria need a dev check continues at [11](11-deploy-and-acceptance.md); any other non-product issue keeps its column and closes from Разработка ([non-product work](non-product-work.md)). Read `TA: Doc- after merge`; when a `Doc-` page was named, its draft may start now ([12](12-docs-after-merge.md)): the run goes only in `Agent` ([row lifecycle](../elements/memory.md#row-lifecycle)).

### Repair while the queue is stopped
The single definition of the stopped queue; other files link here.
- **Stop:** a failed «Deploy to dev» on `main` (red, or a `cancelled` latest run with no later run queued or in progress, [events](events.md#deploy-wait)) stops the merge queue from the wake that sees it; memory records it in the `Queue:` line ([memory](../elements/memory.md#what-it-holds)). No later merge can turn that run green. «CI» runs only on PRs: Deploy repeats the .NET test suite on `main`, while the frontend suite and the PR-chip script stay PR checks (`ci.yml`). Red «CI» on a PR is handled on that PR and never stops the queue. «Secret scan» on push to `main` is not a stop or resume condition; every next PR is still gated by its own «Secret scan» in the [ready predicate](#ready-predicate).
- **Repair, in that wake, ahead of epic children:** when nothing in the repo is broken (a cancelled run, or a runner or network flake), one re-run of that failed run id for the same commit (`gh run rerun <run id>`, not a rollback; the automatic deploy completing, not a manual deploy). The orchestrator starts it; when its token cannot trigger workflows, it asks the owner with the [owner question](../templates/owner/owner-question.md) template and records a [repair row](../elements/memory.md#repair-row) (re-asked by the silence policy, not stored like a hold). Otherwise, exactly one repair item: a backlog item, or a child of the current epic when the failure belongs there ([bugs](bugs.md)); only its one PR merges while the queue is stopped; it passes the ready predicate and the full review on its own head sha and does not wait for the red run to go green.
- **One re-run per failed run id:** if that attempt does not end as a green Deploy whose deployed sha is the repair commit, open the one repair item and allow only its PR; a second failure is never another re-run of the same failure.
- **Server-side cause:** fixed inside the one repair, not a second repair path: the server change needs the owner's yes, asked through the [repair row](../elements/memory.md#repair-row); a re-run of the same commit afterwards needs no yes ([orchestrator §5.14](../orchestrator.md#514-server-and-dev)).
- **Resume:** the queue stays stopped, including while the re-run or the repair Deploy is queued or in progress, until a successful Deploy whose deployed sha is the repair commit (the re-run commit, or the repair merge) or a descendant of it. A non-red latest run (in progress, or cancelled with a newer run) does not resume it. A red repair Deploy is a new failure under this same rule.

**Exit gate:** merged with every gate recorded; the issue stays Разработка (or Приёмка for a fix during acceptance) until Deploy to dev is green; then Приёмка ([11](11-deploy-and-acceptance.md)) only for items that need a dev check.

## Safety-net wakes
A CI or Secret-scan wake on a PR whose agent run is still live is expected → sleep after never-stall checks and confirming memory. A Deploy wake is never slept: it goes through the [Deploy wait](events.md#deploy-wait) and the [event table](events.md#events). A wake with no live agent, a second branch, or Dependabot → act per [events](events.md).
