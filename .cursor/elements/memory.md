# Short memory (durable cache)
Box file `/home/box/shared/travelagency/work/memory.md`, section «TravelAgency». Not in git.

A **small cache** of in-flight facts that must survive compaction or token loss. It is not a backlog and not a second board. **Board and GitHub remain the source of truth** for columns, labels and the backlog.

## What it holds
One table (or short lists) with:

| Field | Required when | Meaning |
|---|---|---|
| Issue | always for a row | `#NN` |
| Stage / column | always | current `статус:*` (FSM state) |
| PR | when a PR exists | number and URL |
| Sha | when waiting on CI/Deploy | head sha before merge, merge sha after |
| Agent | when a cloud agent owns the stage | agent id |
| Waiting on | when blocked | `agent` \| `CI+Automations` \| `Deploy` \| `owner` \| `none` |
| Next | always | the next concrete step |
| Pause / why | when paused | why paused and what resumes it (owner answer, resume word) |
| Notes | optional | one line |

Also allowed as short bullets under the table: active agents list, open owner questions (issue + question text), paused epics.

## Update rules
- **Write** when an item is taken, advanced, paused, waiting on CI/Automations/Deploy/owner, when an agent is launched or finishes, when the owner answers, and when a wait clears.
- **Prune** when an issue closes, a pause ends, an agent is dismissed, or a wait is done. Do not accumulate history; closed items leave the file.
- **Read** on every wake, before new work, and when refreshing memory (at least once a day and at the weekly audit).
- A CI/CD safety-net wake is matched to rows by sha and agent ([events](../lifecycle/events.md#wake-sources)).

## Relation to the FSM
The column on the issue is the state. Memory only records **waits and pauses inside** that state so the orchestrator can resume. An owner question does **not** change the column; it sets `Waiting on` = owner and Pause/why.

## Template (file body)
```markdown
# TravelAgency: short memory

Updated: <YYYY-MM-DD HH:MM Europe/Moscow>

| Issue | Stage | PR | Sha | Agent | Waiting on | Next | Pause / why |
|---|---|---|---|---|---|---|---|
| #NN | <column> | #PR or - | <sha> or - | <id> or - | owner / agent / CI+Automations / Deploy / none | <step> | <why / what resumes> or - |

## Open owner questions
- #NN: <question one line>

## Notes
- <one-liners or none>
```
