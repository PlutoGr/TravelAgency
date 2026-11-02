# CI/CD
Responsible: the orchestrator (reads results, merges, reacts). The cloud agent that owns a PR watches that PR's CI and Automations until ready. Workflow edits go through a PR by a cloud agent with the [devops brief](../briefs/devops.md).

| Workflow | When | Role |
|---|---|---|
| `ci.yml` «CI»: `.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR` | every PR (not push to `main`) | merge gate: must be green |
| `gitleaks.yml` «Secret scan» | PR and push to `main` | merge gate: must be green |
| `deploy.yml` «Deploy to dev» | push to `main` (= merge), manual with rollback sha | automatic deploy to dev; the next merge waits until it is green, except the one repair PR for a failed Deploy on `main` ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)); a successful run uploads artifact `deployed-sha` (the commit it deployed) after its last step; manual run or rollback needs the owner's yes |
| `notify-cicd.yml` «Notify orchestrator» | after «CI», «Deploy to dev» and «Secret scan» runs | **safety-net** wake for routine «TravelAgency CI/CD» ([events](../lifecycle/events.md#wake-sources)); the payload carries `deployed_sha` for Deploy ([Deploy wait](../lifecycle/events.md#deploy-wait)); fails visibly when its secret is missing |
| `link-closed-by.yml` «Link PR on issue close» | issue closed | sets the PR chip after close ([done](board/columns/done.md)) |

Board hygiene / `board-sync` / `board_order` are deleted; the orchestrator alone sets labels and Status (hygiene deleted / orchestrator-only).

- Merge = deploy: one PR at a time; while a failed Deploy on `main` stops the queue, only its repair PR ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)).
- Do not merge while «CI» or «Secret scan» for the head sha is still running or red.
- Primary path to review: the cloud agent reports ready ([ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate)); the safety-net wake confirms or catches misses.

## Red run
While CI is red, do not merge. Who fixes:
- **Feature / backlog PR with a cloud agent:** that agent classifies and fixes (flake: one rerun; in-feature failure: fix; fixture/upstream: fix or question).
- **Dependabot or unowned red CI / failed or orphan-cancelled Deploy on `main`:** orchestrator opens or takes a [backlog](../project.md#standing-backlog) item in the wake that sees it and runs the FSM (thin stages OK) ([bugs](../lifecycle/bugs.md)); red PR CI never stops the queue; for a failed Deploy on `main` its PR is the one merge allowed while the queue is stopped ([repair](../lifecycle/10-review-and-merge.md#repair-while-the-queue-is-stopped)).

Ask the owner only to merge despite red CI, or when the failure is a breaking change, a peer conflict or outside the task. A later green run uses the normal merge gates.

## Other rules
- Depth of review by risk: trivial (docs, typos, trivial config) and normal code = CI + first-line Automations + orchestrator full review; risky (infra, CI, Docker/nginx, auth, data and migrations, secrets handling, runtime or network dependencies) = same plus escalated security review ([review and merge](../lifecycle/10-review-and-merge.md#3-escalation-review)).
- First-line review is Cursor Automations ([automations](automations.md)); dependency PRs: [dependencies](dependencies.md).
- Servers: read logs and status freely; any change, manual deploy or rollback needs the owner's yes, configs backed up first.
