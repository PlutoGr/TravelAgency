# 12 Doc- after merge (before Готово)
Not a column. `Doc-` describes what works in `main`, so its text is written after merge and never before.

**Entry:** the merged PR named a `Doc-` page in «Документация»; or `TA: Doc- after merge` reported a gap.

**Who:** a cloud agent with the [docs writer brief](../briefs/docs-writer.md) drafts the text; the orchestrator reviews and publishes.

**Steps**
1. Read `TA: Doc- after merge` on the merged PR ([automations](../elements/automations.md)). A gap comment is a checklist item, not the page text. Do not skip this because the automation was silent; do not duplicate a page already on the wiki. When several PRs merged the same day, scan them all.
2. Write or fix the named page ([Doc- pages](../elements/wiki/doc-pages.md), [Doc- template](../templates/wiki/doc-page.md)); a decision becomes `Doc-ADR-NNN-<Name>` ([ADR template](../templates/wiki/adr-page.md)).
3. Mark the `Feature-` page «сделано» with links (`Doc-`, ADR, PRs); the label remains the status. The `Epic-` page is marked only after owner acceptance ([13](13-epic-acceptance.md)).
4. Wiki push checklist ([wiki](../elements/wiki/README.md#push-checklist)).

When the PR says «вики не затронута», do not invent a page. Merge never waits for this edit, but Готово does.

**Exit gate:** the named page is live and the gap comment answered; then Готово.
