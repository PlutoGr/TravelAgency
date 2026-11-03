# CI/CD
Responsible: the orchestrator (reads results, merges, reacts). The cloud agent that owns a PR watches that PR's CI and Automations until ready. Workflow edits go through a PR by a cloud agent with the [devops brief](../briefs/devops.md).

| Workflow | When | Role |
|---|---|---|
| `ci.yml` «CI»: `Изменения кода`, `.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR` | every PR (not push to `main`), manual run | merge gate: must be green; on a [docs-only PR](../lifecycle/10-review-and-merge.md#ready-predicate) the two build-and-test jobs are skipped (counts as success); a failed `Изменения кода` runs them; a manual run runs everything |
| `gitleaks.yml` «Secret scan» | PR and push to `main` | merge gate: must be green |
| `deploy.yml` «Deploy to dev» | push to `main` (= merge), manual with rollback sha | automatic deploy to dev; the next merge waits until it is green, except the one repair PR for a failed Deploy on `main` ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)); a successful run uploads artifact `deployed-sha` (the commit it deployed, kept 90 days; memory [`Last deploy:`](memory.md#what-it-holds) records it) after its last step; manual run or rollback needs the owner's yes |
| `notify.yml` «Notify orchestrator» | one `pr-checks` event when required checks on a same-repo PR head have completed; one `deploy` event only for a `push` of «Deploy to dev» on this repository's `main` | meaningful wake for routine «TravelAgency CI/CD» ([events](../lifecycle/events.md#webhook-payload)); secrets `GROK_BOT_CICD_WEBHOOK_URL` and `GROK_BOT_CICD_WEBHOOK_KEY`; fails visibly when a send has no secret |
| `link-closed-by.yml` «Link PR on issue close» | issue closed | sets the PR chip after close ([done](board/columns/done.md)) |

Board hygiene / `board-sync` / `board_order` are deleted; the orchestrator alone sets labels and Status (hygiene deleted / orchestrator-only).

- Merge = deploy: one PR at a time; while a failed Deploy on `main` stops the queue, only its repair PR ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)).
- Do not merge while «CI» or «Secret scan» for the head sha is still running or red. A docs-only PR needs no Automations wait ([ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate)).
- Primary path to review: the cloud agent reports ready ([ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate)). A webhook is acted on only when it matches `Waiting on`, or it is a Dependabot `pr-checks` event, or it is `deploy` ([webhook match](../lifecycle/events.md#webhook-match)). A `pr-checks` event that does not match exits without logging.

## Red run
While CI is red, do not merge. Who fixes:
- **Feature / backlog PR with a cloud agent:** that agent classifies and fixes (flake: one rerun; in-feature failure: fix; fixture/upstream: fix or question).
- **Dependabot red `pr-checks`:** open a bug issue from the bug template ([dependencies](dependencies.md), [bugs](../lifecycle/bugs.md)). A green Dependabot PR follows that file, including which majors may merge and when docker or github-actions need the owner's yes. Red PR checks never stop the queue.
- **Failed or orphan-cancelled Deploy on `main`:** follows the [repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped) choice: one re-run, or one repair item whose PR is the only merge while the queue is stopped.

Ask the owner only to merge despite red CI, or when the failure is a breaking change, a peer conflict or outside the task. A later green run uses the normal merge gates.

## Other rules
- Depth of review follows [escalation review](../lifecycle/10-review-and-merge.md#3-escalation-review). No second risk list here.
- First-line review is Cursor Automations ([automations](automations.md)); dependency PRs: [dependencies](dependencies.md).
- Servers: read logs and status freely; any change, manual deploy or rollback needs the owner's yes, configs backed up first.
