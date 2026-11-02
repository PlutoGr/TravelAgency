# Cursor Automations
Active project automations post GitHub comments as `cursor`. They are the first-line review while the issue stays in Разработка. They do not notify the orchestrator chat, do not approve or merge, do not write wiki pages, move cards or change `статус:*`, and do not replace the columns, Приёмка or Doc- after merge. Private URLs and dashboard notes: box-only ops file.

## Inventory (keep Active)
| Automation | When | Gate |
|---|---|---|
| `TA: PR review` | PR open/push | before merge: process, format, Doc-, obvious bugs |
| `TA: Find vulnerabilities` | PR open/push | before merge: security findings (comment only) |
| Bugbot (From Cursor) | every push | before merge: bug findings; Autofix Off; scope this repo only |
| `TA: CI failed triage` | checks failed | before merge: only if CI was red; confirm green again |
| `TA: Doc- after merge` | PR merged | after merge: Doc- gap → issue or wiki fix |
| `TA: Weekly vuln scan` | Monday 09:00 MSK | weekly: read run history; open issues if needed |

## Handling
- Before merge: wait until CI and automations have finished, read every finding including medium and lower, reply on each thread ([review replies](../templates/github/review-replies.md), [review and merge](../lifecycle/10-review-and-merge.md#3-findings-reply-before-merge)). A finding a newer commit already fixed still gets «исправлено» with the head sha, not a new debate.
- Do not merge while a blocker is open (format, Doc- line, red CI, a validated security or bug finding without a finished reply).
- Do not re-do the same checklist in a second pass; escalate only per [escalation review](../lifecycle/10-review-and-merge.md#4-escalation-review).
- After merge: `TA: Doc- after merge` per [Doc- after merge](../lifecycle/12-docs-after-merge.md).
- Weekly: [weekly audit](../lifecycle/weekly-audit.md) item 5.
- Adding, removing or reconfiguring an automation is a repo-settings change: owner's yes.
