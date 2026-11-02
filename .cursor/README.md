# .cursor: TravelAgency development rules

One unified rule set for this project. English for rules; GitHub artifacts produced by the process (issues, comments, commits, PRs, wiki) are Russian and impersonal ([core](rules/00-core.mdc)).

Read from general to detailed:

| Level | Path | What it holds |
|---|---|---|
| 0 | [rules/00-core.mdc](rules/00-core.mdc), [rules/security.mdc](rules/security.mdc) | Always applied: language, public wording, secrets, map |
| 1 | [orchestrator.md](orchestrator.md) | The controller: guiding principle, mission, duties, powers, boundaries, anti-stall |
| 1 | [project.md](project.md) | Project facts: repo, stack, environments, product themes, parallel work |
| 2 | [lifecycle/](lifecycle/README.md) | The development lifecycle: overview, then one file per stage, events, weekly audit |
| 3 | [elements/](elements/README.md) | How to work with each element: board and columns, issues, labels, git, PRs, CI/CD, automations, wiki, roadmap, cloud agents |
| 4 | [templates/](templates/README.md) | Every template as a separate file. Other files link here instead of inlining |
| 4 | [briefs/](briefs/README.md) | Role briefs for cloud agents (who the agent is, bar, boundaries, deliverable) |

Precedence on conflict: the repo state wins over any description; within this package the more specific file wins (template > element > lifecycle stage > orchestrator), except that [00-core](rules/00-core.mdc), [security](rules/security.mdc) and the owner-approval list in [orchestrator.md](orchestrator.md#5-powers-act-and-report-vs-ask-first) always win. A conflict found is itself a defect: fix it ([orchestrator.md](orchestrator.md#1-guiding-principle)).

Not committed (see `.gitignore`): `mcp.json`, `*.env`, chats, caches.
