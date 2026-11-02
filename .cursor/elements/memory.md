# Short memory (durable cache)
Box file `/home/box/shared/travelagency/work/memory.md`, section «TravelAgency». Not in git.

A **small cache** of in-flight facts that must survive compaction or token loss. It is not a backlog and not a second board. **Board and GitHub remain the source of truth** for columns, labels and the backlog. This file is the single source for the row fields, the Sha cell, the `Waiting on` values, the `Question` field and the row lifecycle; other files link here.

## What it holds
One table (or short lists) with:

| Field | Required when | Meaning |
|---|---|---|
| Issue | always for a row | `#NN`, or `-` for a Dependabot / unowned PR row or the [repair row](#repair-row) |
| Stage / column | always | current `статус:*` (FSM state), or `Dependabot` / `repair` / PR label when Issue is `-` |
| PR | when a PR exists | number and URL |
| Sha | when a PR exists or was merged | the [Sha cell](#sha-cell) format |
| Agent | while a cloud agent run is live | agent id of the live run only ([agent rule](#row-lifecycle)) |
| Waiting on | always | the mechanical wait, one value of the [vocabulary](#waiting-on-values) |
| Question | always (`-` when none) | the open owner question or hold ([Question](#question)) |
| Next | always | the next concrete step (never empty) |

Under the table: a `Queue:` line while the merge queue is stopped (`stopped since run <id>; repair: re-run <run id> | #NN | repair row`), removed only when the queue resumes ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)). Also allowed as short bullets: active agents list, open owner questions (issue + question text), paused epics.

## Sha cell
One format for every row (product, non-product, Dependabot):
- Before merge: `head=<40-hex>` (the PR head; CI wakes match it).
- After merge: `entry=<40-hex> fix=<40-hex|->`. `entry` = the merge that first left Разработка for Приёмка or the Deploy wait; `fix` = the latest fix merge during Приёмка, else `-` (always `-` on Dependabot and non-product rows without a fix).
- While a fix PR during Приёмка is open: `entry=<40-hex> fix=<40-hex|-> head=<40-hex>`; CI wakes match `head`. On the fix merge keep `entry`, set `fix` to that fix merge and drop `head`; a fix merge is never written as `fix=-`.
- After a rollback the cell ends with ` rolled-back` until the match sha is deployed again ([rollback](#row-lifecycle)).
- **Match sha** = `fix` when set, else `entry`. The Deploy wait, the 60-minute catch-up (that PR's `mergedAt`), the rollback check and re-entry after a rollback all use the match sha.
- **Contains:** a sha is contained when it is the deployed sha or its ancestor (`git merge-base --is-ancestor <sha> <deployed sha>`), with the deployed sha read per [Deploy wait](../lifecycle/events.md#deploy-wait).

## `Waiting on` values
Only mechanical waits; an owner question or hold lives in [Question](#question), never here.
- `agent`: a live cloud agent run is the wait (its deliverable is the next step).
- `CI+Automations`: Automations still running after green CI on a PR no live agent watches (Dependabot). Red CI is not this wait; it follows the unowned-red path.
- `Deploy`: merged, waiting for a green «Deploy to dev» whose deployed sha contains the match sha. Only a feature, bug-feature, non-product or Dependabot row waits on Deploy; never an epic.
- `issue #NN`: parked until issue #NN is Готово; `Next` = resume the parked step (for example acceptance of the parent, or taking a blocked child). Resumed by the [close procedure](board/columns/done.md) of #NN.
- `children`: an epic whose children carry the work: in Разбиение after the split is accepted and until the first child is taken, and in В работе while any child is open. `Next` = В работе on the first take, or the [epic demo](../lifecycle/13-epic-acceptance.md) on the last child close. No orchestrator step until a child is taken or closed; resumed by the take or by the child-close step of the [close procedure](board/columns/done.md). It never starts the epic demo by itself, except a missed resume: if every child is already Готово on a wake, start the epic demo on that wake.
- `none`: the `Next` step is actionable by the orchestrator now (once `Question` is `-`). Never use `none` for a mechanical wait.

The never-stall loop performs only `none` rows with `Question` = `-`; `issue #NN` and `children` rows are resumed by the close procedure (a missed resume is done at once: the issue is already closed, or every child is already Готово).

An “orchestrator review” is `none` with `Next` = full review. Use that shape only when the review can start now: a Dependabot PR whose Automations are still running stays `Waiting on` = `CI+Automations` ([dependencies](dependencies.md)). Dependabot row: `Issue` = `-`, `Stage` = `Dependabot`, `PR`, `Sha` = `head=…`, `Waiting on` = `CI+Automations`, `Next` = check Automations, then full review; after the switch `none` with `Next` = full review; after merge `Deploy` with `entry=… fix=-`.

## Question
`-`, or exactly one of:
- an owner question: `<question> (asked: <issue comment or chat>)`, with ` agent <id>` appended when a cloud run ended `RESULT blocked` on it (the reply goes to that run);
- `epic demo` (the owner demo ask, [epic demo](../lifecycle/13-epic-acceptance.md));
- `create Status «Демо»` (the Project Status option is missing);
- the [repair row](#repair-row) ask;
- `hold: <reason>; resume: <condition>; restore «<column>»` for [Удержание](board/columns/on-hold.md).

Rules:
- **Blocks transitions:** while `Question` is not `-`, no column transition of that issue happens on any wake (child close, Deploy, rollback, audit, missed resume, never-stall loop). Only bookkeeping that does not change the column continues (the Sha cell, `Agent`, `Waiting on` = `Deploy` becoming `none` with `Next` = the pending transition). The pending transition is performed in the answer turn if its condition still holds (re-check then).
- **Answer:** an owner reply clears only the `Question` it answers (the [owner answer](../lifecycle/events.md#events) event); then re-evaluate the row and perform the pending transition if its condition holds now. A reply that does not answer it keeps it.
- **Re-ask:** the silence policy re-asks every non-empty `Question` except `hold:` on memory-refresh and the Monday audit only ([orchestrator §8](../orchestrator.md#8-never-stall)); a hold is listed in the weekly audit with its resume condition and never re-asked.
- One `Question` per row. A second owner question about the same issue waits until the first is answered; a question about other work goes on that work's row.

## Repair row
Created only for an owner ask of the [repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped) (the token cannot re-run the failed run, or a server-side cause needs the owner's yes); it never rewrites another row. `Issue` = `-`, `Stage` = `repair`, `PR` = `-`, `Sha` = `-`, `Agent` = `-`, `Waiting on` = `none`, `Question` = the ask, `Next` = observe `rerun <run id>` (or the new Deploy after the server change). The `Queue:` line points at it. Delete it only when that run id is observed queued, in progress or succeeded (or the server change is applied and a new Deploy is running); a clarifying reply that does not start it keeps the row and its `Question`. The `Queue:` line stays until the queue resumes.

## Row lifecycle
The single definition of how a row changes; stage and event files link here.
- **Create** when an item is taken, when a Dependabot PR's CI is green ([dependencies](dependencies.md)), for a [repair row](#repair-row), and when an issue is reopened (in the column its close comment names, [done](board/columns/done.md)).
- **Rewrite, keep the row:** every other transition (advance, owner question asked or answered, hold or resume, agent launch, finish or dismissal, wait cleared, rollback) rewrites `Waiting on`, `Question` and `Next` as needed (`Waiting on` and `Next` always written) and keeps the row while the issue is open.
- **Delete** only when the issue is closed and no Deploy wait remains, when a Dependabot row (`Issue` = `-`) is consumed by its green Deploy, or when a [repair row](#repair-row)'s run is observed. Nothing else deletes a row; closed items then leave the file. Do not accumulate history.
- **Agent:** one live run per issue in `Agent` (finish or cancel a live run, for example a `Doc-` draft during Приёмка, before launching another). Launching never overwrites a mechanical wait: `Waiting on` = `agent` only when the agent is the wait; otherwise it keeps `Deploy` or `issue #NN`. On the run's finish or error wake clear `Agent` and keep the mechanical wait. A run that ended `RESULT blocked` on an owner-needed question: `Question` = that question with ` agent <id>`, `Waiting on` = `none`, `Next` = reply to that run. In the turn that sends the reply (or relaunches), set `Agent` to the resumed run, clear the `Question`, and set `Waiting on` = `agent` when the agent is again the wait.
- **Green Deploy containing the match sha** (a `Deploy` row): feature, bug-feature or non-product issue with a dev check → Приёмка, `Waiting on` = `none`, `Next` = finish acceptance; non-product issue without a dev check → close in this wake when its exit holds ([non-product work](../lifecycle/non-product-work.md)), else `Waiting on` = `agent` while a `Doc-` draft run is live, otherwise `none` with `Next` = the remaining step (publish the named page, check the criteria) and close; a closed issue or a Dependabot row → delete. Until that green Deploy a draft run is only in `Agent` and `Waiting on` stays `Deploy`.
- **Rollback after advance:** an open Приёмка row is rolled back only when the latest completed successful Deploy is an older sha that does not contain its match sha, and no newer «Deploy to dev» whose head contains the match sha is queued or in progress. Then: column Разработка (label, Status and a status comment naming the rollback), Sha cell ` rolled-back`; `Waiting on` = `none` becomes `Deploy` with `Next` = re-enter Приёмка; a `Deploy` wait (a fix waiting for its Deploy) is kept unchanged; `agent` or `issue #NN` is kept. Rows marked ` rolled-back` are checked on every green Deploy: when the match sha is contained again, restore Приёмка, drop the mark, keep the other wait (or `none` with `Next` = finish acceptance).
- **Read** on every wake, before new work, on the weekday memory-refresh routine (`travelagency-memory-refresh`), and at the weekly audit. A CI/CD safety-net wake is matched to rows by sha and agent ([events](../lifecycle/events.md#wake-sources)).

## Relation to the FSM
The column on the issue is the state. Memory only records **waits and questions inside** that state so the orchestrator can resume. An owner question does **not** change the column; it sets `Question`.

## Template (file body)
```markdown
# TravelAgency: short memory

Updated: <YYYY-MM-DD HH:MM Europe/Moscow>

| Issue | Stage | PR | Sha | Agent | Waiting on | Question | Next |
|---|---|---|---|---|---|---|---|
| #NN or - | <column> | #PR or - | head=<sha> or entry=<sha> fix=<sha or -> [head=<sha>] [rolled-back] or - | <id> or - | agent / CI+Automations / Deploy / issue #NN / children / none | - or <question (asked: where)> [agent <id>] or epic demo or create Status «Демо» or hold: <reason>; resume: <condition>; restore «<column>» | <step> |

Queue: stopped since run <id>; repair: re-run <run id> | #NN | repair row   (only while the queue is stopped)

## Open owner questions
- #NN: <question one line>

## Notes
- <one-liners or none>
```
