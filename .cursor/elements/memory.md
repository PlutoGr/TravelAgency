# Short memory (durable cache)
Box file `/home/box/shared/travelagency/work/memory.md`, section «TravelAgency». Not in git.

A **small cache** of in-flight facts that must survive compaction or token loss. It is not a backlog and not a second board. **Board and GitHub remain the source of truth** for columns, labels and the backlog.

## What it holds
One table (or short lists) with:

| Field | Required when | Meaning |
|---|---|---|
| Issue | always for a row | `#NN`, or `-` for a Dependabot / unowned PR row |
| Stage / column | always | current `статус:*` (FSM state), or `Dependabot` / PR label when Issue is `-` |
| PR | when a PR exists | number and URL |
| Sha | when waiting on CI/Deploy | head sha before merge, merge sha after |
| Agent | while a cloud agent run is live | agent id; cleared on that run's finish or error wake |
| Waiting on | always | `agent` \| `CI+Automations` \| `Deploy` \| `owner` \| `issue #NN` \| `none` |
| Next | always | the next concrete step (never empty) |
| Pause / why | when paused | why paused and what resumes it (owner answer, resume word) |

`Waiting on` values:
- `agent`: a live cloud agent run owns the step.
- `CI+Automations`: checks or Automations still running on a PR no live agent watches (Dependabot).
- `Deploy`: merged, waiting for a green «Deploy to dev» whose commit contains the merge sha ([Deploy wait](../lifecycle/events.md#deploy-wait)).
- `owner`: an owner question or the epic demo is open; re-asked only on memory-refresh and the Monday audit.
- `issue #NN`: parked until issue #NN is Готово; `Next` = resume the parked step (for example acceptance of the parent). Resumed by the [close procedure](board/columns/done.md) of #NN; the never-stall loop does not execute it.
- `none`: the `Next` step is actionable by the orchestrator now. Never use `none` for a wait.

An “orchestrator review” is `none` with `Next` = full review. Use that shape only when the review can start now: a Dependabot PR whose Automations are still running stays `Waiting on` = `CI+Automations` ([dependencies](dependencies.md)).

Dependabot row `Issue` = `-`, `Stage` = `Dependabot`, `PR` = number, `Sha` = head sha, `Waiting on` = `CI+Automations` with `Next` = check Automations, then full review; after the switch `none` with `Next` = full review; after merge `Deploy` with the merge sha.

Also allowed as short bullets under the table: active agents list, open owner questions (issue + question text), paused epics.

## Update rules
- **Write** when an item is taken, advanced, paused, waiting on CI/Automations/Deploy/owner, when an agent is launched or finishes, when the owner answers, and when a wait clears. Always write both `Next` and `Waiting on`.
- **Prune** when an issue closes, a pause ends, an agent is dismissed, or a wait is done. Do not accumulate history; closed items leave the file.
- **Read** on every wake, before new work, on the weekday memory-refresh routine (`travelagency-memory-refresh`), and at the weekly audit.
- A CI/CD safety-net wake is matched to rows by sha and agent ([events](../lifecycle/events.md#wake-sources)).

## Relation to the FSM
The column on the issue is the state. Memory only records **waits and pauses inside** that state so the orchestrator can resume. An owner question does **not** change the column; it sets `Waiting on` = owner and Pause/why.

## Template (file body)
```markdown
# TravelAgency: short memory

Updated: <YYYY-MM-DD HH:MM Europe/Moscow>

| Issue | Stage | PR | Sha | Agent | Waiting on | Next | Pause / why |
|---|---|---|---|---|---|---|---|
| #NN or - | <column> | #PR or - | <sha> or - | <id> or - | owner / agent / CI+Automations / Deploy / issue #NN / none | <step> | <why / what resumes> or - |

## Open owner questions
- #NN: <question one line>

## Notes
- <one-liners or none>
```
