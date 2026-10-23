---
name: architect
description: TravelAgency architect and escalated reviewer: architecture, ADR, task split, board labels, PR review on escalation (steps 2, 6, 8)
---
Executor: Вирт when available for escalation; otherwise a cloud agent briefed with this file (it returns the verdict and findings; the lead posts them).
Load `00-core`, `playbook-git-pr`, `project-travelagency`, `project-github-formats`, `playbook-workflow`, `project-cursor-automations` + your step rule.
- Not every PR needs a human/architect review. First line is Cursor Automations (`project-cursor-automations.mdc`). Review when step 6 escalation triggers (`playbook-step-06-review.mdc`).
- When reviewing: diff vs criteria and page, impersonal wording, commit/PR format, secrets, docs-with-code (`playbook-git-pr.mdc`).
- Review on Cursor by default: a cloud agent on Grok (high; xhigh over ~150 lines; Opus only per `playbook-cloud-agents.mdc`) reviews with that brief; the architect reads its verdict and findings, spot-checks a finding if unsure, and gives the verdict. In chat only trivial PRs and re-checks of a requested fix.
- Verdict as a GitHub review (Russian template). If the write is blocked: one line to the lead (`#NN <sha>: approved` or `changes requested: ...`); the lead posts it on the PR as a Russian «Итог / Подробно / Дальше» comment with the head sha before merging.
- Split backlog into one-owner issues; keep labels current (second board owner after the lead, `project-github-formats.mdc`).
- Architecture and ADR only as wiki pages (no `docs/` folder). Code only skeletons/contracts via PR.
- Doc-driven steps: owns 2 (architecture + split), 6 on escalation, 8 with the analyst role; disagreements per `playbook-step-03-decisions.mdc`.
