# CI/CD
Responsible: the orchestrator (reads results, merges, reacts). Workflow edits go through a PR by a cloud agent with the [devops brief](../briefs/devops.md).

| Workflow | When | Role |
|---|---|---|
| `ci.yml` «CI»: `.NET сборка и тесты`, `Фронтенд сборка и тесты`, `Скрипт чипа PR` | every PR | merge gate: must be green |
| `gitleaks.yml` «Secret scan» | PR and push to `main` | merge gate: must be green |
| `deploy.yml` «Deploy to dev» | push to `main` (= merge), manual with rollback sha | automatic deploy to dev; the next merge waits until it is green; manual run or rollback needs the owner's yes |
| `notify-cicd.yml` «Notify orchestrator» | after CI and Deploy runs | drives the «TravelAgency CI/CD» routine, the only CI wake ([events](../lifecycle/events.md)) |
| `board-sync.yml` «Board sync» | issue events, daily, manual | label → column sync and card order ([board](board/README.md)) |
| `link-closed-by.yml` «Link PR on issue close» | issue closed | sets the PR chip after close ([done](board/columns/done.md)) |

- Merge = deploy: one PR at a time.
- Red CI: no merge; red CI is an ask-before-merge case. Read only failing lines.
- Depth of review by risk: trivial (docs, typos, trivial config) and normal code = CI + first-line review; risky (infra, CI, Docker/nginx, auth, data and migrations, secrets handling, runtime or network dependencies) = CI + first-line review + escalated security review ([review and merge](../lifecycle/10-review-and-merge.md#4-escalation-review)).
- First-line review is Cursor Automations ([automations](automations.md)); dependency PRs: [dependencies](dependencies.md).
- Servers: read logs and status freely; any change, manual deploy or rollback needs the owner's yes, configs backed up first.
