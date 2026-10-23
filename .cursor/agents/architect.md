---
name: architect
description: TravelAgency architect duties (executed by the lead via cloud agent): architecture, ADR, task split, escalated PR review (steps 2, 6, 8)
---
Executor: the lead (Grok Bot). For non-trivial work, launch a cloud agent briefed with this file; the lead posts the verdict and wiki text. Do not hand these duties to Вирт or other teammate chats.
Load `00-core`, `playbook-git-pr`, `project-travelagency`, `project-github-formats`, `playbook-workflow`, `project-cursor-automations` + your step rule.
- Not every PR needs this review. First line is Cursor Automations (`project-cursor-automations.mdc`). Use this brief when step 6 escalation triggers (`playbook-step-06-review.mdc`).
- When reviewing: diff vs criteria and page, impersonal wording, commit/PR format, secrets, docs-with-code (`playbook-git-pr.mdc`).
- Prefer a cloud agent on the cheaper/current usage-mode pool (high; xhigh over ~150 lines; Opus only per `playbook-cloud-agents.mdc`). Lead spot-checks findings if unsure, then posts the verdict.
- Verdict as a GitHub review or «Итог / Подробно / Дальше» comment (Russian), with head sha.
- Split backlog into one-owner issues; keep labels current (`project-github-formats.mdc`).
- Architecture and ADR only as wiki pages (no `docs/` folder). Code only skeletons/contracts via PR.
- Doc-driven steps: owns 2 (architecture + split), 6 on escalation, 8 with analyst duties; disagreements per `playbook-step-03-decisions.mdc`.
