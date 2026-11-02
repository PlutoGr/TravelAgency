# Short memory (durable cache)
Box file `/home/box/shared/travelagency/work/memory.md`, section «TravelAgency». Not in git.

A **small cache** of in-flight facts that must survive compaction or token loss. It is not a backlog and not a second board. **Board and GitHub remain the source of truth** for columns, labels and the backlog.

## What it holds
One table (or short lists) with:

| Field | Required when | Meaning |
|---|---|---|
| Issue | always for a row | `#NN`, or `-` for a Dependabot / unowned PR row or the [repair row](#repair-row) |
| Stage / column | always | current `статус:*` (FSM state), or `Dependabot` / `repair` / PR label when Issue is `-` |
| PR | when a PR exists | number and URL |
| Sha | when a PR exists or was merged | the [Sha cell](#sha-cell) format |
| Agent | while a cloud agent run is live | agent id of the live run only ([agent rule](#row-lifecycle)) |
| Waiting on | always | one value of the [vocabulary](#waiting-on-values) |
| Next | always | the next concrete step (never empty) |
| Pause / why | when paused | why paused and what resumes it (owner answer, resume word); also the blocked agent id and the rollback condition ([row lifecycle](#row-lifecycle)) |

Under the table: a `Queue:` line while the merge queue is stopped (`stopped since run <id>; repair: re-run <run id> | #NN | repair row`), removed only when the queue resumes ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)). Also allowed as short bullets: active agents list, open owner questions (issue + question text), paused epics.

## Sha cell
One format for every row (product, non-product, Dependabot):
- Before merge: `head=<40-hex>` (the PR head; CI wakes match it).
- After merge: `entry=<40-hex> fix=<40-hex|->`. `entry` = the merge that first left Разработка for Приёмка or the Deploy wait; `fix` = the latest fix merge during Приёмка, else `-` (always `-` on Dependabot and non-product rows without a fix).
- While a fix PR during Приёмка is open: `entry=<40-hex> fix=<40-hex|-> head=<40-hex>`; CI wakes match `head`. On the fix merge keep `entry`, set `fix` to that fix merge and drop `head`; a fix merge is never written as `fix=-`.
- **Match sha** = `fix` when set, else `entry`. The Deploy wait, the 60-minute catch-up (that PR's `mergedAt`), the rollback check and re-entry after a rollback all use the match sha.
- **Contains:** a sha is contained when it is the deployed sha or its ancestor (`git merge-base --is-ancestor <sha> <deployed sha>`), with the deployed sha read per [Deploy wait](../lifecycle/events.md#deploy-wait).

## `Waiting on` values
This is the single vocabulary; other files link here.
- `agent`: a live cloud agent run is the wait (its deliverable is the next step).
- `CI+Automations`: Automations still running after green CI on a PR no live agent watches (Dependabot). Red CI is not this wait; it follows the unowned-red path.
- `Deploy`: merged, waiting for a green «Deploy to dev» whose deployed sha contains the match sha. Only a feature, bug-feature, non-product or Dependabot row waits on Deploy; never an epic.
- `owner`: an owner question, the epic demo, the request to create Status «Демо» or a [repair row](#repair-row) ask is open (re-asked only on memory-refresh and the Monday audit), or an owner hold ([Удержание](board/columns/on-hold.md)) waits for the resume word (not re-asked).
- `issue #NN`: parked until issue #NN is Готово; `Next` = resume the parked step (for example acceptance of the parent). Resumed by the [close procedure](board/columns/done.md) of #NN.
- `children`: an epic in В работе while any child is open; `Next` = stage 13 on the last child close. No orchestrator step until then: resumed by the child-close step of the [close procedure](board/columns/done.md). It never starts [stage 13](../lifecycle/13-epic-acceptance.md) by itself, except a missed resume: if every child is already Готово on a wake, start stage 13 on that wake. An epic in В работе with every child Готово while Status «Демо» is missing waits on `owner` with Pause/why = create Status «Демо» (stage 13); a missed-resume check or a memory refresh never rewrites that `owner` row.
- `none`: the `Next` step is actionable by the orchestrator now. Never use `none` for a wait.

The never-stall loop performs only `none` rows; `issue #NN` and `children` rows are resumed by the close procedure (a missed resume is done at once: the issue is already closed, or every child is already Готово).

An “orchestrator review” is `none` with `Next` = full review. Use that shape only when the review can start now: a Dependabot PR whose Automations are still running stays `Waiting on` = `CI+Automations` ([dependencies](dependencies.md)). Dependabot row: `Issue` = `-`, `Stage` = `Dependabot`, `PR`, `Sha` = `head=…`, `Waiting on` = `CI+Automations`, `Next` = check Automations, then full review; after the switch `none` with `Next` = full review; after merge `Deploy` with `entry=… fix=-`.

## Repair row
Created only for an owner ask of the [repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped) (the token cannot re-run the failed run, or a server-side cause needs the owner's yes); it never rewrites another row. `Issue` = `-`, `Stage` = `repair`, `PR` = `-`, `Sha` = `-`, `Agent` = `-`, `Waiting on` = `owner`, `Next` = `rerun <run id>` once the owner triggered it (or apply the server change after the owner's yes), Pause/why = the question (re-asked by the silence policy). The `Queue:` line points at it. On the owner answer, or when the re-run is observed, delete this row; the `Queue:` line stays until the queue resumes.

## Row lifecycle
The single definition of how a row changes; stage and event files link here.
- **Create** when an item is taken, when a Dependabot PR's CI is green ([dependencies](dependencies.md)), for a [repair row](#repair-row), and when an issue is reopened (in the column its close comment names, [done](board/columns/done.md)).
- **Rewrite, keep the row:** every other transition (advance, pause, pause end, owner answer, agent launch, finish or dismissal, wait cleared, rollback) rewrites `Waiting on` and `Next` (always both) and keeps the row while the issue is open.
- **Delete** only when the issue is closed and no Deploy wait remains, when a Dependabot row (`Issue` = `-`) is consumed by its green Deploy, or when a [repair row](#repair-row) is answered or its re-run observed. Nothing else deletes a row; closed items then leave the file. Do not accumulate history.
- **Agent:** one live run per issue in `Agent` (finish or cancel a live run, for example a `Doc-` draft during Приёмка, before launching another). Launching never overwrites an external wait: `Waiting on` = `agent` only when the agent is the wait; otherwise it keeps `Deploy`, `issue #NN` or `owner`. On the run's finish or error wake clear `Agent` and keep the external wait. When a run ends `RESULT blocked` on an owner-needed question, `Waiting on` = `owner` and Pause/why keeps `agent <id>` until the reply is sent or the run is relaunched. In the turn that sends the reply (or relaunches), set `Agent` to the resumed run, drop the id from Pause/why, and set `Waiting on` = `agent` when the agent is again the wait.
- **Green Deploy containing the match sha** (a `Deploy` row): feature, bug-feature or non-product issue with a dev check → Приёмка, `Waiting on` = `none`, `Next` = finish acceptance; non-product issue without a dev check → close in this wake when its exit holds ([non-product work](../lifecycle/non-product-work.md)), else `Waiting on` = `agent` while a `Doc-` draft run is live, otherwise `none` with `Next` = the remaining step (publish the named page, check the criteria) and close; a closed issue or a Dependabot row → delete. Until that green Deploy a draft run is only in `Agent` and `Waiting on` stays `Deploy`.
- **Rollback after advance** (checked on every green Deploy): an open Приёмка row, whatever its `Waiting on` (including `none`), whose match sha the deployed sha does not contain → column Разработка (label, Status and a status comment naming the rollback). `Waiting on` = `none` becomes `Deploy` with `Next` = re-enter Приёмка when the deployed sha contains the match sha. Any other wait (`agent`, `issue #NN`, `owner`) is kept; Pause/why gets `rolled back: re-enter Приёмка when <match sha> is deployed`, and such rows are checked on every green Deploy. When contained again: restore Приёмка and the kept wait (or `none` with `Next` = finish acceptance) and drop the rollback note.
- **Read** on every wake, before new work, on the weekday memory-refresh routine (`travelagency-memory-refresh`), and at the weekly audit. A CI/CD safety-net wake is matched to rows by sha and agent ([events](../lifecycle/events.md#wake-sources)).

## Relation to the FSM
The column on the issue is the state. Memory only records **waits and pauses inside** that state so the orchestrator can resume. An owner question does **not** change the column; it sets `Waiting on` = owner and Pause/why.

## Template (file body)
```markdown
# TravelAgency: short memory

Updated: <YYYY-MM-DD HH:MM Europe/Moscow>

| Issue | Stage | PR | Sha | Agent | Waiting on | Next | Pause / why |
|---|---|---|---|---|---|---|---|
| #NN or - | <column> | #PR or - | head=<sha> or entry=<sha> fix=<sha or -> [head=<sha>] or - | <id> or - | agent / CI+Automations / Deploy / owner / issue #NN / children / none | <step> | <why / what resumes> or - |

Queue: stopped since run <id>; repair: re-run <run id> | #NN | repair row   (only while the queue is stopped)

## Open owner questions
- #NN: <question one line>

## Notes
- <one-liners or none>
```
