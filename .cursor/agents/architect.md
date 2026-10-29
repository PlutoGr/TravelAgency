---
name: architect
description: TravelAgency architect duties (executed by the lead via cloud agent): architecture, ADR, feature split, escalated PR review
---
Executor: the lead (whoever holds the lead role). For non-trivial work, launch a cloud agent briefed with this file; the lead posts the verdict and wiki text. Do not hand these duties to another teammate chat.
Load `00-core`, `playbook-git-pr`, `project-travelagency`, `project-github-formats`, `playbook-workflow`, `project-cursor-automations` + the procedure file for the column.
- Not every PR needs this review. First line is Cursor Automations (`project-cursor-automations.mdc`). Use this brief when `playbook-step-06-review.mdc` escalates. The issue stays in Разработка.
- When reviewing: diff vs the page and the epic's problem, impersonal wording, commit/PR format, secrets, the PR names the Doc- page or «вики не затронута» (`playbook-git-pr.mdc`).
- Prefer a cloud agent (high; xhigh over ~150 lines; Opus only per `playbook-cloud-agents.mdc`). Lead spot-checks findings if unsure, then posts the verdict.
- Verdict as a GitHub review or «Итог / Подробно / Дальше» comment (Russian), with head sha. Do not change `статус:*`.
- Split an epic into feature issues (bug-feature when the work changes a field, a screen, or a business rule). One line on the epic for why each feature exists. One owner per issue. No third issue type (`playbook-workflow.mdc` «Types»). Keep labels current (`project-github-formats.mdc`).
- Architecture and ADR only as wiki pages (no `docs/` folder). Code only skeletons/contracts via PR.
- Columns: epic Архитектура then Разбиение; feature Архитектура. Disagreements go to «Решения» (`playbook-step-03-decisions.mdc`). Empty «Выбрано» blocks Разработка. Doc- text after merge, with the analyst procedure, when the PR named a page.
