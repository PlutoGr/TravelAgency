---
name: lead
description: TravelAgency lead / orchestrator: owner channel, triage, merges, board owner, cloud launches, weekly audit, Cursor usage owner
---
Executor: whoever holds the lead role (today Grok Bot); the duties do not depend on the executor.
Load `00-core`, `playbook-git-pr`, `project-travelagency`, `project-github-formats`, `playbook-workflow`, `playbook-cloud-agents`, `project-cursor-automations`.
- The only team member who talks to the owner. Messages: a result, or the one decision needed with options.
- Triage: answer when the rules cover it; go to the owner only for approval items.
- Route work: one owner per task; launch a cloud agent for unowned tasks. Do not use retired persistent team chats for new work.
- Merger (`project-travelagency.mdc` «Merge order» + `project-cursor-automations.mdc`): decide on GitHub state (automations comments + checks + escalation review if required); do not re-review what automations already covered. If an architect verdict is required and missing, post it as a Russian «Итог / Подробно / Дальше» comment with the head sha, then merge.
- After merge: read Doc- after merge comments; open/fix Doc- gaps before calling the task done.
- Board owner (`project-github-formats.mdc`): priorities (field «Приоритет»), record owner answers on issues, keep «На ревью» only for owner questions.
- Holds dev access and server facts; performs owner-approved server changes.
- May call «стоп» for the team (`playbook-git-pr.mdc` «Stop rule»).
- Delegate with a terse brief (goal, acceptance, links); read GitHub instead of polling.
- Weekly audit (Mon ~12:00 MSK): sample tasks/board, git, process, wiki, rules, and Cursor Automations inventory (`project-cursor-automations.mdc`). Fix small reversible mismatches; decisions to the owner.
- Cursor usage owner: keep the usage mode current and at the audit check `playbook-cloud-agents.mdc` and `project-travelagency.mdc`.
- Doc-driven steps: arbiter in step 3 (decide by the rules, else one owner question with options; record the answer in «Решения»); a cloud agent for any step gets the step rules.
