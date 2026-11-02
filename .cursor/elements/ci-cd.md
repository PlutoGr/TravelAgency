# CI/CD
Responsible: the orchestrator (reads results, merges, reacts). The cloud agent that owns a PR watches that PR's CI and Automations until ready. Workflow edits go through a PR by a cloud agent with the [devops brief](../briefs/devops.md).

| Workflow | When | Role |
|---|---|---|
| `ci.yml` «CI»: `.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR` | every PR | merge gate: must be green |
| `gitleaks.yml` «Secret scan» | PR and push to `main` | merge gate: must be green |
| `deploy.yml` «Deploy to dev» | push to `main` (= merge), manual with rollback sha | automatic deploy to dev; the next merge waits until it is green; manual run or rollback needs the owner's yes |
| `notify-cicd.yml` «Notify orchestrator» | after «CI», «Deploy to dev» and «Secret scan» runs | **safety-net** wake for routine «TravelAgency CI/CD» ([events](../lifecycle/events.md#wake-sources)); fails visibly when its secret is missing |
| `board-sync.yml` «Board sync» | daily, manual | board **hygiene only** (add missing cards, archive, order within column); never changes status ([board](board/README.md#board-hygiene)) |
| `link-closed-by.yml` «Link PR on issue close» | issue closed | sets the PR chip after close ([done](board/columns/done.md)) |

- Merge = deploy: one PR at a time.
- Do not merge while «CI» or «Secret scan» for the head sha is still running or red.
- Primary path to review: the cloud agent reports ready after all checks and bot reviews finish; the safety-net wake confirms or catches misses.

## Red run
While CI is red, do not merge. Who fixes:
- **Feature / backlog PR with a cloud agent:** that agent classifies and fixes (flake: one rerun; in-feature failure: fix; fixture/upstream: fix or question).
- **Dependabot or unowned red CI / red on `main`:** orchestrator opens or takes a [backlog](../project.md#standing-backlog) item and runs the FSM (thin stages OK) ([bugs](../lifecycle/bugs.md)).

Ask the owner only to merge despite red CI, or when the failure is a breaking change, a peer conflict or outside the task. A later green run uses the normal merge gates.

## Other rules
- Depth of review by risk: trivial (docs, typos, trivial config) and normal code = CI + first-line Automations + orchestrator full review; risky (infra, CI, Docker/nginx, auth, data and migrations, secrets handling, runtime or network dependencies) = same plus escalated security review ([review and merge](../lifecycle/10-review-and-merge.md#3-escalation-review)).
- First-line review is Cursor Automations ([automations](automations.md)); dependency PRs: [dependencies](dependencies.md).
- Servers: read logs and status freely; any change, manual deploy or rollback needs the owner's yes, configs backed up first.
