# Doc- pages
`Doc-` describes only what works in `main`. Template: [doc page](../../templates/wiki/doc-page.md); decisions: [ADR](../../templates/wiki/adr-page.md).

- **When:** after merge, for the page the PR named ([Doc- after merge](../../lifecycle/12-docs-after-merge.md)). Never before `main`.
- **Who:** docs writer cloud agent drafts, the orchestrator publishes.
- **Bar:** every claim provable by code in `main`. An unprovable claim moves to `Plan-` or is dropped; an unverified one gets «требует проверки» with an issue link, removed when the issue closes.
- **ADR:** `Doc-ADR-NNN-<Name>`, next free number, numbers never reused; written when a «Решения» row with lasting architectural impact ships.
- Existing groups: system, API, services, how it is implemented, reference ([sidebar](sidebar.md)).
