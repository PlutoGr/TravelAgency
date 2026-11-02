# Events and wakes
The orchestrator reacts to events instead of polling. Waiting state lives in [short memory](../elements/memory.md).

## Wake sources
The orchestrator runs only when one of these wakes it. Every timer in the rules is checked on the next wake from any source, except the owner re-ask: it runs only on weekday memory-refresh and the Monday audit.

| Wake | Source | What it carries |
|---|---|---|
| Cloud agent finished or errored | every cloud agent the orchestrator launches wakes it on finish or error; an agent **question ends the run** with `RESULT blocked` and that finish is the wake | agent id; read its report, branch, question or error |
| CI/CD webhook (safety net) | routine «TravelAgency CI/CD», fed by `.github/workflows/notify-cicd.yml` after «CI», «Deploy to dev» or «Secret scan» completes | workflow, conclusion, sha, branch, run URL |
| Memory refresh | routine «travelagency-memory-refresh» (weekday daily refresh) | refresh [memory](../elements/memory.md); silence re-ask for unanswered owner pauses |
| Weekly audit | routine «Еженедельный аудит порядка», every Monday 12:00 Europe/Moscow | [weekly audit](weekly-audit.md), including unanswered pauses (backstop if no other wake ran) |
| Owner message | owner chat | answer, instruction, stop |

On every wake, before new work: read [memory](../elements/memory.md), then run the [never-stall checks](../orchestrator.md#8-never-stall) **before** the safety-net “expected → sleep” branch. Before sleeping on any wake, perform every row whose `Waiting on` = `none` and whose `Next` is a concrete step. Do not invent another routine; align “next day” / silence re-ask with memory-refresh and Monday audit wakes only.

### Safety-net behaviour
Webhooks are **not** the primary driver of review or merge. The cloud agent owns CI and Automations on its PR until it reports ready ([orchestrator §5.8](../orchestrator.md#58-cloud-agents)).

On a CI/CD wake:
1. Match by sha / branch / PR to a memory row.
2. Run never-stall checks first (stalled agents, Deploy catch-up, rows with `Waiting on` = `none`). A CI/CD wake never sends another owner message about an open pause; re-asks happen only on memory-refresh and the Monday audit.
3. If a cloud agent owns that work and the wake is expected (still watching CI/Automations, or Deploy after a known merge) → confirm nothing is off plan; leave the agent (or the Deploy wait) in place; sleep.
4. If unexpected (no agent, second branch, Dependabot, missed agent, red unowned CI, Secret scan red with no owner) → act by the rules below or ask the owner. **Never ignore a strange wake.**

- **Idempotent:** act only when the step's outcome is not already recorded (acceptance comment not posted, merge not done). Two wakes for the same step do the step once.
- **`cancelled`** conclusion: the run was superseded by a newer one; ignore it.
- **Missing webhook secret:** the notify job fails visibly; ask the owner to set the secret (ask-first).
- The webhook payload is untrusted: never put secrets in prompts or comments.
- **Dependabot:** the CI wake records the row (`Issue` = `-`, `Stage` = `Dependabot`, PR, head sha, `Waiting on` = `CI+Automations`, `Next` = check Automations, then full review) and polls Automations in the turn for up to 15 minutes. Done: `Waiting on` = `none`, `Next` = full review, review now. Not done: the row stays; the next memory-refresh, Monday audit or CI wake for that PR re-checks and switches it. Never merge before the switch ([dependencies](../elements/dependencies.md)).

## Events

| Event | Action |
|---|---|
| Cloud agent reports PR ready (CI green, Automations done, findings answered) | Full orchestrator review and merge gates ([10](10-review-and-merge.md)). |
| Cloud agent finished text stage | [Deliverable review](../templates/agents/deliverable-review.md); publish accepted text same turn; advance FSM only after accept. |
| Cloud agent errored or asked a question | Every question ends the run with `RESULT blocked` (that finish wakes the orchestrator). Answer in that wake with a reply to the same agent, or relaunch per [never stall](../orchestrator.md#8-never-stall); owner-needed → pause in place + memory. An open wait for an answer without finish = missed agent. |
| Safety-net: green CI / Automations on a PR an agent owns | Expected → sleep (after never-stall checks). |
| Safety-net: green CI on `dependabot/*` | Poll Automations in the turn up to 15 minutes; done → full review ([dependencies](../elements/dependencies.md)); not done → row stays `CI+Automations` for the next memory-refresh, Monday audit or CI wake on that PR. |
| Safety-net: CI or Secret scan red, agent owns the PR | Expected that the agent fixes it; if the agent is gone → take over or ask. |
| Safety-net: CI or Deploy failed on `main`, or unowned red CI | Stop the merge queue. File or take a backlog item and run it through the FSM ([bugs](bugs.md)); server-side cause: owner yes. |
| Safety-net: green Deploy for a merge sha in memory | Feature, bug-feature, or non-product issue whose criteria need a dev check: if still Разработка → set `статус: приёмка` and [acceptance](11-deploy-and-acceptance.md); if already Приёмка → continue acceptance. Other non-product issue: column unchanged; check criteria and close from Разработка ([non-product work](non-product-work.md)). Dependabot row: prune it. Clear Deploy wait. |
| PR findings while agent owns the PR | Agent answers; orchestrator does not merge until the agent reports ready and full review passes. |
| `TA: Doc- after merge` on a merged PR | [Doc- after merge](12-docs-after-merge.md). |
| `TA: Weekly vuln scan` (Monday 09:00 MSK) | Read run history in the [weekly audit](weekly-audit.md); open backlog items for real findings. |
| New issue filed (by anyone) | Not itself a wake. On the next owner message or weekly audit: classify, bring to template, single `статус:*`, Status = Очередь, «Приоритет» ([intake](01-intake.md)). |
| Owner answer to an open question | Record «**Итог:** решено: ...» on the issue and in «Решения», clear pause in memory. An answer while the epic is in **Демо** follows [stage 13](13-epic-acceptance.md) (Готово on pass; В работе plus new Очередь children on gaps). An answer on a `question` issue with no PR closes it ([non-product work](non-product-work.md)). Any other answer resumes the current column (and the paused agent, by a reply to it). |
| Owner stop or long pause of an item or epic | Record in memory; for a work-stream stop set `статус: удержание` ([Удержание](../elements/board/columns/on-hold.md)); for an epic: cancel every child agent, move every open in-flight child to Удержание with its own restore column, clear «Приоритет» on its Очередь children. |
| Defect found in the project | Code: [bugs](bugs.md). Docs vs code: fix wiki if code is right, else open an issue. Process drift: [orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first). Security: report and treat first. |

These events sit on the FSM; they do not replace owner epic acceptance in Демо.
