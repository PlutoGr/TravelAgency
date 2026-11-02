# CI/CD
Responsible: the orchestrator (reads results, merges, reacts). Workflow edits go through a PR by a cloud agent with the [devops brief](../briefs/devops.md).

| Workflow | When | Role |
|---|---|---|
| `ci.yml` «CI»: `.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR` | every PR | merge gate: must be green |
| `gitleaks.yml` «Secret scan» | PR and push to `main` | merge gate: must be green |
| `deploy.yml` «Deploy to dev» | push to `main` (= merge), manual with rollback sha | automatic deploy to dev; the next merge waits until it is green; manual run or rollback needs the owner's yes |
| `notify-cicd.yml` «Notify orchestrator» | after «CI», «Deploy to dev» and «Secret scan» runs | drives the «TravelAgency CI/CD» routine ([wake sources](../lifecycle/events.md#wake-sources)); fails visibly when its secret is missing |
| `board-sync.yml` «Board sync» | issue events, daily, manual | label → column sync and card order; fails on a label conflict or a missing Status option ([board](board/README.md)) |
| `link-closed-by.yml` «Link PR on issue close» | issue closed | sets the PR chip after close ([done](board/columns/done.md)) |

- Merge = deploy: one PR at a time.
- Do not merge while «CI» or «Secret scan» for the head sha is still running.

## Red run
While CI is red, do not merge. Read only the failing lines, then classify:
- **Flake** (passes on rerun, no code cause): one rerun. A second red is not a flake.
- **Expired fixture, upstream image digest or other cause outside the issue** (red on `main` too): a standalone [urgent fix](../lifecycle/non-product-work.md#urgent-fix); never patch it inside a feature branch.
- **Inside the issue's change:** steer the same agent to fix it.

Ask the owner only to merge despite red CI, or when the failure is a breaking change, a peer conflict or outside the task. A later green run uses the normal merge gates.

## Other rules
- Depth of review by risk: trivial (docs, typos, trivial config) and normal code = CI + first-line review; risky (infra, CI, Docker/nginx, auth, data and migrations, secrets handling, runtime or network dependencies) = CI + first-line review + escalated security review ([review and merge](../lifecycle/10-review-and-merge.md#4-escalation-review)).
- First-line review is Cursor Automations ([automations](automations.md)); dependency PRs: [dependencies](dependencies.md).
- Servers: read logs and status freely; any change, manual deploy or rollback needs the owner's yes, configs backed up first.
