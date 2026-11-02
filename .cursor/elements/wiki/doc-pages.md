# Doc- pages
`Doc-` describes only what works in `main`. Template: [doc page](../../templates/wiki/doc-page.md); decisions: [ADR](../../templates/wiki/adr-page.md).

- **When:** after merge, for the page the PR named ([Doc- after merge](../../lifecycle/12-docs-after-merge.md)). Never before `main`, and never a page that describes unmerged code. Малая: the developer writes the draft in the PR; the orchestrator publishes that draft right after merge. Крупная: the draft is written after merge.
- **Who:** Малая, the orchestrator publishes the PR draft (no docs writer). Крупная, a docs writer cloud agent drafts and the orchestrator publishes.
- **Bar:** every claim provable by code in `main`. An unprovable claim moves to `Plan-` or is dropped; an unverified one gets «требует проверки» with an issue link, removed when the issue closes.
- **ADR:** `Doc-ADR-NNN-<Name>`, next free number, numbers never reused; written when a «Решения» row with lasting architectural impact ships.
- Existing groups: system, API, services, how it is implemented, reference ([sidebar](sidebar.md)).
