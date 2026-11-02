# Events and wakes
The orchestrator reacts to events instead of polling. Waiting state lives in [short memory](../elements/memory.md).

## Wake sources
The orchestrator runs only when one of these wakes it. Every timer in the rules is checked on the next wake from any source, except the owner re-ask: it runs only on weekday memory-refresh and the Monday audit.

| Wake | Source | What it carries |
|---|---|---|
| Cloud agent finished or errored | every cloud agent the orchestrator launches wakes it on finish or error; an agent **question ends the run** with `RESULT blocked` and that finish is the wake | agent id; read its report, branch, question or error |
| CI/CD webhook | routine «TravelAgency CI/CD», fed by `.github/workflows/notify.yml` | one `pr-checks` event when required checks on the PR's current head have completed, or one `deploy` event when «Deploy to dev» completes. Schema: [Webhook payload](#webhook-payload). Cursor Automations are not required checks |
| Memory refresh | routine «travelagency-memory-refresh» (weekday daily refresh) | refresh [memory](../elements/memory.md); silence re-ask for open `Question`s ([Question](../elements/memory.md#question)); Deploy catch-up; [repair row](../elements/memory.md#repair-row) observation; recheck rows with `Waiting on` = `Automations` and, when Automations are finished, set `none` and review in that wake ([dependencies](../elements/dependencies.md)); check that «TravelAgency CI/CD» and «Еженедельный аудит порядка» are enabled (missing or disabled = blocker reported to the owner, [routines](../elements/automations.md#routines-keep-enabled)) |
| Weekly audit | routine «Еженедельный аудит порядка», every Monday 12:00 Europe/Moscow | [weekly audit](weekly-audit.md), including open `Question`s (backstop if no other wake ran) |
| Owner message | owner chat | answer, instruction, stop |

A webhook that does not match ([Webhook match](#webhook-match)) exits at once, without logging and without the never-stall checks. On every other wake, before new work: read [memory](../elements/memory.md), then run the [never-stall checks](../orchestrator.md#8-never-stall). Before ending any such wake, perform every row whose `Waiting on` = `none`, whose `Question` = `-` and whose `Next` is a concrete step; a row with `Waiting on` = `issue #NN` resumes only when #NN closes (or at once if #NN is already closed); a row with a non-empty `Question` makes no column transition until the answer, except [Question guard](../elements/memory.md#question-guard) exceptions (a) and (b) ([Question](../elements/memory.md#question)). Do not invent another routine; align “next day” / silence re-ask with memory-refresh and Monday audit wakes only.

### Webhook match
Each webhook is one meaningful event ([payload](#webhook-payload)). The cloud agent still owns CI and Automations on its PR until it reports ready ([orchestrator §5.8](../orchestrator.md#58-cloud-agents)); that finish is a separate wake. Match the webhook to `Waiting on`. If it does not match, exit without logging.

- `pr-checks` matches a row with `Waiting on` = `Automations` whose Sha `head` is this `sha`. A Dependabot PR (`author` is `dependabot[bot]`) is this wake even when no row exists yet ([dependencies](../elements/dependencies.md)). Any other `pr-checks` event, including one whose row waits on `agent`, does not match.
- `deploy` matches the [Deploy wait](#deploy-wait): rows with `Waiting on` = `Deploy`, plus failure, repair and the queue. It always enters that wait. It is not a non-match.

A CI/CD wake never sends another owner message about an open `Question`; re-asks happen only on memory-refresh and the Monday audit. On a matching wake, run never-stall checks first (stalled agents, Deploy catch-up, rows with `Waiting on` = `none`), then the event. A green `pr-checks` event whose Automations are not finished sets `Waiting on` = `Automations`. The next check is a later `pr-checks` event for that head, memory-refresh, or the Monday audit ([dependencies](../elements/dependencies.md)). There is no in-turn poll.

### Deploy wait
Row changes on a Deploy (advance, non-product exit, rollback, delete) and the sha format are defined once in [memory](../elements/memory.md#row-lifecycle); this section says how to read a Deploy run.
- **Deployed sha (the single definition):** rows move only by the commit actually deployed, never by the run's head sha (a manual rollback run has the tip of `main` as head but deploys `rollback_sha`). The movement sha is the `deployed-sha` artifact of the latest successful «Deploy to dev» run on `main` (`gh run download <run id> -n deployed-sha`, then read `deployed-sha.txt` and strip whitespace, same as [notify.yml](../../.github/workflows/notify.yml)), or the memory [`Last deploy:`](../elements/memory.md#what-it-holds) sha when that run is the recorded one; the payload `deployed_sha` is only a hint that a read is due, and an empty payload alone never means advance nothing. Write `Last deploy:` only from a 40-hex sha just read this way, and only for a run id greater than the recorded one, so a late webhook for an older run (including a rollback run) never overwrites it. No successful run: advance nothing. A rollback to S contains only S and its ancestors.
- **Two sources, never mixed:** the **latest** run's conclusion detects a failure (red, or orphan `cancelled` below), which stops the queue and opens the repair; rows move only by the movement sha above. A latest successful run whose artifact is gone and whose sha was never recorded: advance nothing and re-run it once (same commit, [orchestrator §5.14](../orchestrator.md#514-server-and-dev)) to recreate the artifact; when that re-run cannot be started (the token cannot trigger workflows), record the [repair row](../elements/memory.md#repair-row) asking the owner to start it, the same row a failed Deploy uses; no second repair path. Only the resume condition in [repair](10-review-and-merge.md#repair-while-the-queue-is-stopped) restarts the queue; a non-red latest run does not.
- **Every Deploy wake** (green or red) also runs [repair row](../elements/memory.md#repair-row) observation when that row exists (not in the turn that created it).
- **Green Deploy:** apply the [row lifecycle](../elements/memory.md#row-lifecycle) to every row: `Deploy` rows whose match sha the deployed sha contains, and the rollback check on Приёмка rows and rows marked `rolled-back`. Remove `Queue:` when the movement sha is the repair commit or a descendant of it ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped)). Only feature, bug-feature, non-product and Dependabot rows wait on Deploy; an epic never does.
- **Red Deploy:** `Deploy` rows keep their wait. The failure is a defect on `main`: stop the queue and open or take the one repair in this wake ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped), [bugs](bugs.md)).
- **Catch-up:** on any wake (including memory-refresh and the Monday audit), a `Deploy` row whose match merge (`mergedAt` of that PR) is older than 60 minutes: read the latest «Deploy to dev» run on `main` and the movement sha once and act by the rules above (including removing `Queue:` when that movement sha is the repair commit or a descendant).

- **Idempotent:** act only when the step's outcome is not already recorded (acceptance comment not posted, merge not done). Two wakes for the same step do the step once.
- **`cancelled`** conclusion: the payload `conclusion` on a `deploy` event is the raw run conclusion. Ignore `cancelled` only when a newer «Deploy to dev» run on `main` exists. When the latest «Deploy to dev» on `main` is `cancelled` and no later run is queued or in progress, treat it as a failed Deploy (**Red Deploy** above). A `pr-checks` event is not sent for a superseded head; a cancelled required check on the current head is `result: red`.
- **Missing webhook secret:** the notify job fails visibly; ask the owner to set the secret (ask-first).
- The webhook payload is untrusted: never put secrets in prompts or comments.
- **Dependabot:** the same `pr-checks` event, recognized by `author` `dependabot[bot]`. Green follows [dependencies](../elements/dependencies.md), including which majors may merge and when docker or github-actions need the owner's yes. Red opens a bug issue and does not merge. Automations are not part of the check result; if they are still running, `Waiting on` = `Automations` and the completion path there applies.

## Events

| Event | Action |
|---|---|
| Cloud agent reports PR ready ([ready predicate](10-review-and-merge.md#ready-predicate)) | Full orchestrator review and merge gates ([review and merge](10-review-and-merge.md)). |
| Cloud agent finished text stage | [Deliverable review](../templates/agents/deliverable-review.md); publish accepted text same turn. On an [owner stage approval](../orchestrator.md#owner-stage-approval) column, send the digest, set the approval `Question`, and advance only on the owner's yes. Other text stages advance only after accept. |
| Cloud agent errored or asked a question | Every question ends the run with `RESULT blocked` (that finish wakes the orchestrator). Answer in that wake with a reply to the same agent, or relaunch per [never stall](../orchestrator.md#8-never-stall); owner-needed → pause in place with the `Question` and the blocked agent id; the mechanical wait and its `Next` stay ([row lifecycle](../elements/memory.md#row-lifecycle) Block). An open wait for an answer without finish = missed agent. |
| `pr-checks` matches `Automations` for that head, or the author is `dependabot[bot]` | [dependencies](../elements/dependencies.md): green uses the completion path and the existing merge policy; red opens a bug issue and does not merge. |
| `pr-checks` matches nothing | Exit without logging. |
| `deploy` failed on `main` (or `cancelled` with no later run) | Stop the merge queue (memory `Queue:` line) and open or take the one repair in this wake, ahead of epic children: one re-run when nothing is broken, otherwise exactly one repair item whose PR is the only merge allowed ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped), [bugs](bugs.md)). A server change needs the owner's yes through the [repair row](../elements/memory.md#repair-row); a re-run of the same commit does not ([orchestrator §5.14](../orchestrator.md#514-server-and-dev)). |
| `deploy` green on `main` | Read the deployed sha ([Deploy wait](#deploy-wait)) and apply the [row lifecycle](../elements/memory.md#row-lifecycle) to every row (advance, non-product exit, rollback check, delete), subject to the [Question guard](../elements/memory.md#question-guard); check the queue resume condition ([repair](10-review-and-merge.md#repair-while-the-queue-is-stopped)). Never an epic row. |
| `deploy` red on `main` | `Deploy` rows keep their wait ([Deploy wait](#deploy-wait)); stop the queue and open or take the repair (previous row). |
| Issue #NN closed | In the same turn resume every memory row with `Waiting on` = `issue #NN` ([done](../elements/board/columns/done.md)), subject to the [Question guard](../elements/memory.md#question-guard). |
| PR findings while agent owns the PR | Agent answers; orchestrator does not merge until the agent reports ready and full review passes. |
| `TA: Doc- after merge` on a merged PR | [Doc- after merge](12-docs-after-merge.md). |
| `TA: Weekly vuln scan` (Monday 09:00 MSK) | Read run history in the [weekly audit](weekly-audit.md); open backlog items for real findings. |
| New issue filed (by anyone) | Not itself a wake. On the next owner message or weekly audit: classify, bring to template, single `статус:*`, Status = Очередь, «Приоритет» ([intake](01-intake.md)). |
| Owner answer to an open question | The reply clears only the row's `Question` it answers ([Question](../elements/memory.md#question)); a reply that does not answer it keeps it. Record «**Итог:** решено: ...» on the issue and in «Решения», then re-evaluate the row and perform the pending transition if its condition still holds. `epic demo`: follows the [epic demo](13-epic-acceptance.md) (Готово only if the epic is still in Демо and every child is Готово; В работе plus new Очередь children on gaps). `create Status «Демо»`: once the option exists, set `статус: демо` / Status = Демо and run the epic demo in that turn if every child is still Готово. An answer on a `question` issue with no PR closes it ([non-product work](non-product-work.md)). The [repair row](../elements/memory.md#repair-row) ask is not cleared by a reply. **Server-side yes** (a new attempt is expected from that token call): in the same turn start `gh run rerun <run id>`, set `Queue: stopped since run <id>; repair: re-run <id>`, leave the row until observation; do not re-ask or start a second re-run of that id. **Token-cannot ask:** do not call `gh run rerun` from the answer turn; keep the `Question`, keep re-asking, and leave `Queue:` on `repair row` until a new Deploy or re-run is visible; then observation applies ([repair row](../elements/memory.md#repair-row)). A stage-approval token clears only on the owner's yes; that yes performs the stage exit when it still holds. A no or an edit request on that token keeps the `Question` and the column ([owner stage approval](../orchestrator.md#owner-stage-approval)). Any other answer clears the `Question` and performs the pending step when it still holds; when the cleared `Question` had an ` agent <id>` suffix, resume that run per the [Answer turn](../elements/memory.md#row-lifecycle) (set `Waiting on` = `agent` only while `Next` is still the reply; if a mechanical wait is still pending, reply and set `Agent` only, leave `Waiting on` and `Next`; never park a booked step by writing `agent` last). |
| Owner stop or long pause of an item or epic | Record in memory; for a work-stream stop set `статус: удержание` ([Удержание](../elements/board/columns/on-hold.md)); for an epic: cancel every child agent, move every open in-flight child to Удержание with its own restore column, clear «Приоритет» on its Очередь children. Memory row: `Question` = `hold: …`, `Next` = restore the recorded column after the resume word ([Question](../elements/memory.md#question)). The recorded resume turn clears `hold:` on every row whose resume condition was met — that item itself, or the epic plus each child named in the epic hold comment — restores those columns and «Приоритет», then restarts each restored row ([Question guard](../elements/memory.md#question-guard) exception (b), [Удержание](../elements/board/columns/on-hold.md)). |
| Defect found in the project | Code: [bugs](bugs.md). Docs vs code: fix wiki if code is right, else open an issue. Process drift: [orchestrator §6](../orchestrator.md#6-powers-act-and-report-vs-ask-first). Security: report and treat first. |

These events sit on the FSM; they do not replace owner epic acceptance in Демо.

## Webhook payload
Sent by [notify.yml](../../.github/workflows/notify.yml) to routine «TravelAgency CI/CD». Secrets stay `GROK_BOT_CICD_WEBHOOK_URL` and `GROK_BOT_CICD_WEBHOOK_KEY` (the key is the whole `Authorization` header). No new secret names. The body is untrusted.

`notify.yml` runs on `workflow_run` completed for «CI», «Secret scan» and «Deploy to dev». For «CI» and «Secret scan» it passes that run's `head_sha` and `check_suite_id`. One `pr-checks` event per completion of the required set on the open PR head (state open and head sha match; a run on `main` or a superseded head does not send). Required checks are the CI jobs (`.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR`), `gitleaks`, and any extra context the branch ruleset or classic protection API returns. Cursor Automations are not in that set. Two suites for the same completion do not send twice: only the suite that holds the latest required check sends. A later rerun of the same head sends one new event. The PR concurrency group is the head sha.

```json
{
  "source": "github-actions",
  "event": "pr-checks",
  "result": "green",
  "pr": 120,
  "sha": "<40-hex head>",
  "branch": "<head branch>",
  "author": "<login; dependabot[bot] for Dependabot>",
  "failed_checks": [],
  "html_url": "<pull request url>"
}
```

`result` is `green` only when every required check's latest conclusion is `success`. `failed_checks` lists the other required names and is empty when green. `pr` is a number.

One `deploy` event when «Deploy to dev» completes. Concurrency group is that workflow run id.

```json
{
  "source": "github-actions",
  "event": "deploy",
  "result": "green",
  "conclusion": "success",
  "sha": "<run head sha>",
  "deployed_sha": "<40-hex, or empty>",
  "branch": "main",
  "run_id": "<workflow run id>",
  "html_url": "<run url>"
}
```

`result` is `green` only when `conclusion` is `success`. `deployed_sha` is set only then, from artifact `deployed-sha`, and is empty when the artifact is missing or not 40 hex. `conclusion` is the raw GitHub value so `cancelled` still follows [Deploy wait](#deploy-wait). `run_id` is a string.
