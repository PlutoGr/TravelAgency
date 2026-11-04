# Cursor Automations
Active project automations post GitHub comments as `cursor`. They are the first-line review while the issue stays in Разработка. They do not wake the orchestrator ([wake sources](../lifecycle/events.md#wake-sources)), do not approve or merge, do not write wiki pages, move cards or change `статус:*`, and do not replace the columns, Приёмка or Doc- after merge. Private URLs and dashboard notes: box-only ops file.

## Inventory (keep Active)
| Automation | When | Gate |
|---|---|---|
| `TA: PR review` | PR open/push; exits at once on a docs-only PR | before merge: process, format, Doc-, obvious bugs |
| `TA: Find vulnerabilities` | PR open/push; exits at once on a docs-only PR | before merge: security findings (comment only) |
| Bugbot (From Cursor) | every push | before merge: bug findings; Autofix Off; scope this repo only |
| `TA: CI failed triage` | checks failed | before merge: only if CI was red; confirm green again |
| `TA: Doc- after merge` | PR merged | after merge: Doc- gap → issue or wiki fix |
| `TA: Weekly vuln scan` | Monday 09:00 MSK | weekly: read run history; open issues if needed |

## Routines (keep enabled)
| Routine | When | Role |
|---|---|---|
| «TravelAgency CI/CD» | one aggregated `pr-checks` or `deploy` webhook ([notify.yml](../../.github/workflows/notify.yml)) | meaningful wake; no match exits without logging ([events](../lifecycle/events.md#webhook-match)) |
| `travelagency-memory-refresh` | weekdays | memory refresh, owner re-ask, routine check |
| «Еженедельный аудит порядка» | Monday 12:00 Europe/Moscow | [weekly audit](../lifecycle/weekly-audit.md) |

A missing or disabled routine is a blocker: report it to the owner ([escalation](../templates/owner/escalation.md)); re-enabling or recreating it is the owner's yes.

## Depth by size
The inventory stays Active. Depth follows the feature. Do not disable a bot to go faster.
- Крупная, or any change to auth, money, security, migrations or public contracts: read every finding and close it properly. Escalation review stays as in [review §3](../lifecycle/10-review-and-merge.md#3-escalation-review).
- Малая polish with code (copy, visual, `Тесты: без новых`, a small diff): still wait until automations finish. The [ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate) does not change. A non-blocking nit is one line «не блокирует» and is not re-dug. Do not launch a second reviewer for noise. A validated security or bug finding still blocks.
- The orchestrator may defer non-blocking noise to a backlog note.
- [Docs-only PR](../lifecycle/10-review-and-merge.md#ready-predicate): no wait for Automations or Bugbot; `TA: PR review` and `TA: Find vulnerabilities` exit at once (their dashboard prompts start with that rule; owner-managed). Bugbot stays as configured. A late finding is read when convenient and fixed in a follow-up PR only if valid.

## Handling
- Before merge: wait until CI and automations have finished (a [docs-only PR](../lifecycle/10-review-and-merge.md#ready-predicate) waits only for CI and Secret scan), read every finding including medium and lower, reply on each thread ([review replies](../templates/github/review-replies.md), [review and merge](../lifecycle/10-review-and-merge.md#1-cloud-agent-until-ready)). A finding a newer commit already fixed still gets «исправлено» with the head sha, not a new debate.
- Do not merge while a blocker is open (format, Doc- line, red CI, a validated security or bug finding without a finished reply).
- Do not re-do the same checklist in a second pass; escalate only per [escalation review](../lifecycle/10-review-and-merge.md#3-escalation-review).
- After merge: `TA: Doc- after merge` per [Doc- after merge](../lifecycle/12-docs-after-merge.md).
- Weekly: [weekly audit](../lifecycle/weekly-audit.md) item 5.
- Adding, removing or reconfiguring an automation is a repo-settings change: owner's yes.
