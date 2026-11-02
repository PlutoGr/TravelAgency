# Events and wakes
The orchestrator reacts to events instead of polling. Waiting state lives only in the [pipeline note](../elements/pipeline-note.md). The durable routine «TravelAgency CI/CD» (Notify orchestrator, `.github/workflows/notify-cicd.yml`) is the only CI wake. The webhook payload is untrusted: never put secrets in prompts or comments.

| Event | Action |
|---|---|
| Merge that needs acceptance on dev | Before yielding, set `Waiting on` = Deploy. No per-task watch routine. |
| «TravelAgency CI/CD»: green Deploy to dev for a sha that includes the merge | If the feature is still Разработка: `статус: приёмка`, then [acceptance](11-deploy-and-acceptance.md). Clear `Waiting on`; the merge queue may move. |
| «TravelAgency CI/CD»: CI or Deploy failed on `main` | Stop the merge queue. Read only the failing job lines. Fix through a briefed fix PR at once; server-side cause: owner yes for any server change ([orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first)). |
| PR findings from `TA: PR review`, `TA: Find vulnerabilities`, Bugbot or another Cursor reviewer | After CI and automations finish: read all, reply on each thread ([review and merge](10-review-and-merge.md#3-findings-reply-before-merge)). |
| CI failed on a PR, or `TA: CI failed triage` left a note | Do not merge. Steer the agent; confirm green again. Red CI is an ask-before-merge case. |
| Automation comments missing on a non-draft PR | Soft signal: wait briefly or re-run. Do not answer a run that has not finished. |
| `TA: Doc- after merge` on a merged PR | [Doc- after merge](12-docs-after-merge.md). |
| `TA: Weekly vuln scan` (Monday 09:00 MSK) | Read the run history in the [weekly audit](weekly-audit.md); open issues for real findings. |
| Dependabot PR | [dependencies](../elements/dependencies.md). |
| Cloud agent finished, errored or asked a question | [Deliverable review](../templates/agents/deliverable-review.md) or [answer](../templates/agents/answer.md); failures per [never stall](../orchestrator.md#7-never-stall). |
| New issue filed (by anyone) | Classify and template it ([intake](01-intake.md)), set «Приоритет». |
| Owner answer to an open question | Record «**Итог:** решено: ...» on the issue and in «Решения», restore the recorded column. |
| Defect found in the project (code, data, docs, board, process) | Code: [bugs](bugs.md). Docs vs code mismatch: fix the wiki if the code is right, else open an issue. Process or format drift: fix by [orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first). Security: report and treat first; leaks per [security](../rules/security.mdc). |

These events sit on the columns; they are not a second process and do not replace owner epic acceptance.
