# .cursor
Process rules for this repo. Public text outside this directory stays impersonal (`rules/00-core.mdc`). Cursor Automations may be named there when clarity needs it. Do not name a role or another agent as the actor.
- `rules/00-core.mdc`: always applied; index of everything else.
- `rules/playbook-*.mdc`: generic dev playbook (git and PR, security, the workflow map, the epic route, the feature route, cloud launch, CI wake). Copy as is to another project.
- `rules/project-*.mdc`: TravelAgency specifics (facts, formats, wiki, automations). Rewrite per project.
- `agents/orchestrator.md`: the only controller.
- `briefs/*.md`: short personas pasted into a cloud launch. Not separate bots.
- `templates/`: issue and PR comment template.
Not committed (see `.gitignore`): `mcp.json`, `*.env`, chats, caches.
