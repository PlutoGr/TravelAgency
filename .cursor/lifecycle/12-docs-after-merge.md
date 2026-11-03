# 12 Doc- after merge (before Готово)
Not a column. The wiki is a separate repo. `Doc-` is a thin durable skeleton of what works in `main`: product overview, main zones and pages, roles and auth, major capabilities. Not a per-field essay. Publish only after merge, and only when behavior the skeleton describes actually changed. Prefer omit: visual polish, copy and an internal change that does not move a documented capability say «вики не затронута». Готово waits until every named page is current. When the PR says «вики не затронута», this stage does not apply and does not block Готово; do not invent a page. A PR with no `Размер` (non-product, including infra and settings) follows the Крупная after-merge draft; if that PR already contains the draft text, publish it in the merge turn with no docs writer.

## Малая
The developer writes the draft in the PR «Документация»: the page name and the draft text. Review checks that text together with the code. There is no docs-writer run and no wiki edit before merge.

Right after merge the orchestrator publishes that draft to the wiki, in the merge turn. A gap comment from `TA: Doc- after merge` is checked against the same draft; the orchestrator fixes the page from it, still without a docs-writer. If the publish was missed, it is done before Готово.

## Крупная
The draft is written after merge, never before, and may be drafted while the issue is still in Разработка (waiting for Deploy) or Приёмка. The drafting run is recorded in memory by the [row lifecycle](../elements/memory.md#row-lifecycle) (only in `Agent`, the external wait kept; one live run per issue).

**Entry:** the merged PR named a `Doc-` page in «Документация»; or `TA: Doc- after merge` reported a gap.

**Who:** a cloud agent with the [docs writer brief](../briefs/docs-writer.md) drafts the text; the orchestrator reviews and publishes.

**Steps**
1. Read `TA: Doc- after merge` on the merged PR ([automations](../elements/automations.md)). A gap comment is a checklist item, not the page text. Do not skip this because the automation was silent; do not duplicate a page already on the wiki. When several PRs merged the same day, scan them all.
2. Write or fix the named page ([Doc- pages](../elements/wiki/doc-pages.md), [Doc- template](../templates/wiki/doc-page.md)): change the lines the merge actually moved, and leave the rest. A decision with lasting architectural impact becomes `Doc-ADR-NNN-<Name>` ([ADR template](../templates/wiki/adr-page.md)). A fix PR merged during Приёмка updates the draft before publish.
3. Wiki push checklist ([wiki](../elements/wiki/README.md#push-checklist)).

Merge never waits for this edit.

**Exit gate (both sizes):** the named page is live and, for Крупная, the gap comment answered; then, once acceptance passed, Готово, where the `Feature-` page gets «сделано» ([done](../elements/board/columns/done.md)).
