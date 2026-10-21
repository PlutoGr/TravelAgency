# .cursor
Agent rules for this repo (the only place in the repo that mentions agents).
- `rules/00-core.mdc`: always applied; index of everything else.
- `rules/playbook-*.mdc`: generic dev playbook (git/PR, security, doc-driven steps 1-8, cloud agents). Copy as is to another project.
- `rules/project-*.mdc`: TravelAgency specifics (facts, roles, formats, wiki). Rewrite per project.
- `agents/*.md`: role subagents (analyst, architect, developer, tester, devops).
Not committed (see `.gitignore`): `mcp.json`, `*.env`, chats, caches.
