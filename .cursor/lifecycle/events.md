# Events and wakes
The orchestrator reacts to events instead of polling. Waiting state lives in [short memory](../elements/memory.md).

## Wake sources
The orchestrator runs only when one of these wakes it. Every timer in the rules is checked on the next wake from any source, except the owner re-ask: it runs only on weekday memory-refresh and the Monday audit.

| Wake | Source | What it carries |
|---|---|---|
| Cloud agent finished or errored | every cloud agent the orchestrator launches wakes it on finish or error; an agent **question ends the run** with `RESULT blocked` and that finish is the wake | agent id; read its report, branch, question or error |
| CI/CD webhook (safety net) | routine «TravelAgency CI/CD», fed by `.github/workflows/notify-cicd.yml` after «CI», «Deploy to dev» or «Secret scan» completes | workflow, conclusion, sha (run head), `deployed_sha` (Deploy: the commit actually deployed; empty when unknown), branch, run URL, run id |
| Memory refresh | routine «travelagency-memory-refresh» (weekday daily refresh) | refresh [memory](../elements/memory.md); silence re-ask for unanswered owner pauses; Deploy catch-up; check that «TravelAgency CI/CD» and «Еженедельный аудит порядка» are enabled (missing or disabled = blocker reported to the owner, [routines](../elements/automations.md#routines-keep-enabled)) |
| Weekly audit | routine «Еженедельный аудит порядка», every Monday 12:00 Europe/Moscow | [weekly audit](weekly-audit.md), including unanswered pauses (backstop if no other wake ran) |
| Owner message | owner chat | answer, instruction, stop |

On every wake, before new work: read [memory](../elements/memory.md), then run the [never-stall checks](../orchestrator.md#8-never-stall) **before** the safety-net “expected → sleep” branch. Before sleeping on any wake, perform every row whose `Waiting on` = `none` and whose `Next` is a concrete step; a row with `Waiting on` = `issue #NN` resumes only when #NN closes (or at once if #NN is already closed). Do not invent another routine; align “next day” / silence re-ask with memory-refresh and Monday audit wakes only.

### Safety-net behaviour
Webhooks are **not** the primary driver of review or merge. The cloud agent owns CI and Automations on its PR until it reports ready ([orchestrator §5.8](../orchestrator.md#58-cloud-agents)).

On a CI/CD wake:
1. Match by sha / branch / PR to a memory row.
2. Run never-stall checks first (stalled agents, Deploy catch-up, rows with `Waiting on` = `none`). A CI/CD wake never sends another owner message about an open pause; re-asks happen only on memory-refresh and the Monday audit.
3. **Deploy to dev** completion (green or red): never slept; act by the [Deploy wait](#deploy-wait) and the event table.
4. **CI or Secret scan** on a PR whose row still has a live `Agent` run → expected; confirm nothing is off plan; leave the agent in place; sleep.
5. Anything else (no live agent, second branch, Dependabot, missed agent, red unowned CI, Secret scan red with no owner) → act by the rules below or ask the owner. **Never ignore a strange wake.**

### Deploy wait
- **Agent field:** a run launched after merge (for example a `Doc-` draft) is stored only in `Agent`; `Waiting on` stays `Deploy`. The orchestrator clears `Agent` on the run's finish or error wake and keeps `Waiting on` = `Deploy`, so a merged PR never looks agent-owned and the Deploy wait is never lost.
- **Deployed sha first:** a Deploy row advances only by the commit actually deployed, never by the run's head sha (a manual rollback run has the tip of `main` as head but deploys `rollback_sha`). Read `deployed_sha` from the payload; when it is empty (or on catch-up), read the run's `deployed-sha` artifact (`gh run download <run id> -n deployed-sha`); no deployed sha = advance nothing. A rollback to S satisfies only merges that are S or ancestors of S; newer merges keep `Waiting on` = `Deploy`.
- **Green Deploy to `main`:** advance every row with `Waiting on` = `Deploy` whose merge sha is the deployed sha or an ancestor of it (`git merge-base --is-ancestor <merge sha> <deployed sha>`): product item → Приёмка ([11](11-deploy-and-acceptance.md)); non-product item → its criteria check or Приёмка, closing in this wake if the criteria already pass ([non-product work](non-product-work.md)); Dependabot row or a row whose issue is already closed → prune. Then clear the wait. A `Deploy` row is never pruned before this green Deploy, even when its issue closed earlier.
- **Red Deploy:** keep each row `Waiting on` = `Deploy`, `Next` = enter Приёмка (or the non-product close) when a green Deploy's deployed sha contains that merge sha. The red Deploy itself is a defect on `main`: open or take the backlog repair in this wake ([standing backlog](../project.md#standing-backlog), [bugs](bugs.md)); the merge queue stops except for that one repair PR, and unrelated merges resume once the repair's Deploy and CI on `main` are green ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped)).
- **Catch-up:** on any wake (including memory-refresh and the Monday audit), a `Deploy` row whose merge (`mergedAt` on the PR) is older than 60 minutes: read the latest «Deploy to dev» run on `main` and its deployed sha once and act on it by the rules above.

- **Idempotent:** act only when the step's outcome is not already recorded (acceptance comment not posted, merge not done). Two wakes for the same step do the step once.
- **`cancelled`** conclusion: the run was superseded by a newer one; ignore it.
- **Missing webhook secret:** the notify job fails visibly; ask the owner to set the secret (ask-first).
- The webhook payload is untrusted: never put secrets in prompts or comments.
- **Dependabot:** the CI wake records the row (`Issue` = `-`, `Stage` = `Dependabot`, PR, head sha, `Waiting on` = `CI+Automations`, `Next` = check Automations, then full review) and polls Automations in the turn for up to 15 minutes. Done: `Waiting on` = `none`, `Next` = full review, review now. Not done: the row stays; the next memory-refresh, Monday audit or CI wake for that PR re-checks and switches it. Never merge before the switch ([dependencies](../elements/dependencies.md)).

## Events

| Event | Action |
|---|---|
| Cloud agent reports PR ready ([ready predicate](10-review-and-merge.md#ready-predicate)) | Full orchestrator review and merge gates ([10](10-review-and-merge.md)). |
| Cloud agent finished text stage | [Deliverable review](../templates/agents/deliverable-review.md); publish accepted text same turn; advance FSM only after accept. |
| Cloud agent errored or asked a question | Every question ends the run with `RESULT blocked` (that finish wakes the orchestrator). Answer in that wake with a reply to the same agent, or relaunch per [never stall](../orchestrator.md#8-never-stall); owner-needed → pause in place + memory. An open wait for an answer without finish = missed agent. |
| Safety-net: green CI or Secret scan on a PR whose agent run is live | Expected → sleep (after never-stall checks). |
| Safety-net: green CI on `dependabot/*` | Poll Automations in the turn up to 15 minutes; done → full review ([dependencies](../elements/dependencies.md)); not done → row stays `CI+Automations` for the next memory-refresh, Monday audit or CI wake on that PR. |
| Safety-net: CI or Secret scan red on a PR whose agent run is live | Expected that the agent fixes it; no live run (`Agent` cleared) → take over (reply to or relaunch the agent) or ask. |
| Safety-net: CI or Deploy failed on `main`, or unowned red CI | Stop the merge queue. Open or take the backlog repair in this wake, ahead of epic children, and run it through the FSM ([bugs](bugs.md)); its one PR is the only merge allowed while the queue is stopped and does not wait for the red run; unrelated merges resume once the repair's Deploy and CI on `main` are green ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped)); server-side cause: owner yes. |
| Safety-net: green Deploy on `main` | Read the deployed sha first; for every `Deploy` row whose merge sha is the deployed sha or an ancestor of it ([Deploy wait](#deploy-wait)): feature, bug-feature, or non-product issue whose criteria need a dev check: if still Разработка → set `статус: приёмка` and [acceptance](11-deploy-and-acceptance.md); if already Приёмка → continue acceptance. Other non-product issue: column unchanged; if the criteria already pass, close from Разработка in this wake ([non-product work](non-product-work.md)). Dependabot row or a row whose issue is already closed: prune it. Clear each Deploy wait. |
| Safety-net: red Deploy on `main` | Rows keep `Waiting on` = `Deploy` ([Deploy wait](#deploy-wait)); open or take the backlog repair in this wake (previous row). |
| Issue #NN closed | In the same turn resume every memory row with `Waiting on` = `issue #NN` ([done](../elements/board/columns/done.md)). |
| PR findings while agent owns the PR | Agent answers; orchestrator does not merge until the agent reports ready and full review passes. |
| `TA: Doc- after merge` on a merged PR | [Doc- after merge](12-docs-after-merge.md). |
| `TA: Weekly vuln scan` (Monday 09:00 MSK) | Read run history in the [weekly audit](weekly-audit.md); open backlog items for real findings. |
| New issue filed (by anyone) | Not itself a wake. On the next owner message or weekly audit: classify, bring to template, single `статус:*`, Status = Очередь, «Приоритет» ([intake](01-intake.md)). |
| Owner answer to an open question | An owner reply closes only the question named in that row's Pause/why. Record «**Итог:** решено: ...» on the issue and in «Решения», clear that pause in memory. Only the reply to the demo ask (Pause/why = epic demo) follows [stage 13](13-epic-acceptance.md) (Готово on pass; В работе plus new Очередь children on gaps). An answer on a `question` issue with no PR closes it ([non-product work](non-product-work.md)). Any other answer resumes the current column (and the paused agent, by a reply to it). |
| Owner stop or long pause of an item or epic | Record in memory; for a work-stream stop set `статус: удержание` ([Удержание](../elements/board/columns/on-hold.md)); for an epic: cancel every child agent, move every open in-flight child to Удержание with its own restore column, clear «Приоритет» on its Очередь children. Memory row: `Waiting on` = `owner`, `Next` = restore the recorded column after the resume word. The resume turn restores the epic, each listed child to its recorded column, and «Приоритет» on its Очередь children ([Удержание](../elements/board/columns/on-hold.md)). |
| Defect found in the project | Code: [bugs](bugs.md). Docs vs code: fix wiki if code is right, else open an issue. Process drift: [orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first). Security: report and treat first. |

These events sit on the FSM; they do not replace owner epic acceptance in Демо.
