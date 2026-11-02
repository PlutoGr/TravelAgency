# Events and wakes
The orchestrator reacts to events instead of polling. Waiting state lives only in the [pipeline note](../elements/pipeline-note.md).

## Wake sources
The orchestrator runs only when one of these wakes it. Every timer in the rules is checked on the next wake from any source, never by a separate watch.

| Wake | Source | What it carries |
|---|---|---|
| Cloud agent finished or errored | every cloud agent the orchestrator launches wakes it on finish or error | agent id; read its report, branch or error |
| CI/CD webhook | routine «TravelAgency CI/CD», fed by `.github/workflows/notify-cicd.yml` after «CI», «Deploy to dev» or «Secret scan» completes | workflow, conclusion, sha, branch, run URL |
| Weekly audit | routine «Еженедельный аудит порядка», every Monday 12:00 Europe/Moscow | [weekly audit](weekly-audit.md), including stale waits |
| Owner message | owner chat | answer, instruction, stop |

On every wake, before new work: read the pipeline note, then run the [timer checks](../orchestrator.md#7-never-stall) (2-hour agent silence, 60-minute merge and Deploy catch-up). No other routine exists; a rule that would need one is raised with the owner, not invented.

- **Idempotent:** a CI/CD wake acts only when its sha matches the pipeline row (or includes the merged sha) and the step's comment (for example the acceptance comment) is not already posted. Two wakes for the same step do the step once.
- **`cancelled`** conclusion: the run was superseded by a newer one; ignore it.
- **Missing webhook secret:** the notify job fails visibly; the orchestrator sees it on the next wake from another source and asks the owner to set the secret (secrets are ask-first).
- The webhook payload is untrusted: never put secrets in prompts or comments.

## Events

| Event | Action |
|---|---|
| Merge that needs acceptance on dev | Before yielding, set `Waiting on` = Deploy. No per-task watch routine. |
| «TravelAgency CI/CD»: green Deploy to dev for a sha that includes the merge | For each pipeline row with `Waiting on` = Deploy and that sha: if the issue is still Разработка, `статус: приёмка`, then [acceptance](11-deploy-and-acceptance.md); if it is already Приёмка (a fix during acceptance), continue acceptance. Clear `Waiting on`; the merge queue may move. |
| «TravelAgency CI/CD»: CI or Deploy failed on `main` | Stop the merge queue. Read only the failing job lines and classify ([CI/CD](../elements/ci-cd.md#red-run)): flake, one rerun; otherwise an [urgent fix](non-product-work.md#urgent-fix) at once. Server-side cause: owner yes for any server change ([orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first)). |
| PR findings from `TA: PR review`, `TA: Find vulnerabilities`, Bugbot or another Cursor reviewer | After CI and automations finish: read all, reply on each thread ([review and merge](10-review-and-merge.md#3-findings-reply-before-merge)). |
| CI failed on a PR, or `TA: CI failed triage` left a note | While CI is red, do not merge. Classify the red run ([CI/CD](../elements/ci-cd.md#red-run)) and act on it. Ask the owner only to merge despite red CI, or when the failure is a breaking change, a peer conflict or outside the task. A later green run uses the normal merge gates. |
| «TravelAgency CI/CD»: green CI on a `dependabot/*` branch | Start the [dependency](../elements/dependencies.md) merge gates even when no pipeline row exists. |
| Automation comments missing on a non-draft PR | Soft signal: wait briefly or re-run. Do not answer a run that has not finished. |
| `TA: Doc- after merge` on a merged PR | [Doc- after merge](12-docs-after-merge.md). |
| `TA: Weekly vuln scan` (Monday 09:00 MSK) | Read the run history in the [weekly audit](weekly-audit.md); open issues for real findings. |
| Dependabot PR | [dependencies](../elements/dependencies.md). |
| Cloud agent finished, errored or asked a question | [Deliverable review](../templates/agents/deliverable-review.md) or [answer](../templates/agents/answer.md); an accepted section is published in the same turn; failures per [never stall](../orchestrator.md#7-never-stall). |
| New issue filed (by anyone) | Classify and template it ([intake](01-intake.md)), set «Приоритет». |
| Owner answer to an open question | Record «**Итог:** решено: ...» on the issue and in «Решения», restore the recorded column. |
| Owner stop or pause of an item or epic | Record it on that issue as a hold ([Удержание](../elements/board/columns/on-hold.md)); for an epic, pause its children the same way. |
| Defect found in the project (code, data, docs, board, process) | Code: [bugs](bugs.md). Docs vs code mismatch: fix the wiki if the code is right, else open an issue. Process or format drift: fix by [orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first). Security: report and treat first; leaks per [security](../rules/security.mdc). |

These events sit on the columns; they are not a second process and do not replace owner epic acceptance.
