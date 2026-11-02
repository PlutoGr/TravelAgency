# 12 Doc- after merge (before Готово)
Not a column. `Doc-` describes what works in `main`, so the wiki publishes only what is in `main`: the text is drafted after merge, never before, and may be drafted while the issue is still in Разработка (waiting for Deploy) or Приёмка. The drafting run is recorded in memory by the [row lifecycle](../elements/memory.md#row-lifecycle) (only in `Agent`, the external wait kept; one live run per issue).

**Entry:** the merged PR named a `Doc-` page in «Документация»; or `TA: Doc- after merge` reported a gap. When the PR says «вики не затронута», this stage does not apply and does not block Готово; do not invent a page.

**Who:** a cloud agent with the [docs writer brief](../briefs/docs-writer.md) drafts the text; the orchestrator reviews and publishes.

**Steps**
1. Read `TA: Doc- after merge` on the merged PR ([automations](../elements/automations.md)). A gap comment is a checklist item, not the page text. Do not skip this because the automation was silent; do not duplicate a page already on the wiki. When several PRs merged the same day, scan them all.
2. Write or fix the named page ([Doc- pages](../elements/wiki/doc-pages.md), [Doc- template](../templates/wiki/doc-page.md)); a decision becomes `Doc-ADR-NNN-<Name>` ([ADR template](../templates/wiki/adr-page.md)). A fix PR merged during Приёмка updates the draft before publish.
3. Wiki push checklist ([wiki](../elements/wiki/README.md#push-checklist)).

Merge never waits for this edit. Готово waits for the acceptance pass and, when a page was named, for this page.

**Exit gate:** the named page is live and the gap comment answered; then, once acceptance passed, Готово, where the `Feature-` page gets «сделано» ([done](../elements/board/columns/done.md)).
