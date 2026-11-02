# Dependencies (Dependabot)
Dependabot PRs have no issue and keep default branch names; commit prefixes per [commits](git/commits.md).

- A safety-net CI wake on a `dependabot/*` branch wakes the orchestrator ([events](../lifecycle/events.md)). Record the PR in [memory](memory.md) with `Issue` = `-`.
- The CI wake **stays in the turn** until Automations have finished or a fixed timeout. If Automations are not done, leave the row with `Waiting on` = `CI+Automations` (or `none` + `Next` = full review after timeout deferral) and review on the next audit. **Never merge in between.**
- **npm and NuGet** (minor, patch and major; majors are separate PRs, groups are minor/patch only): after CI and Automations finish, the orchestrator does a **full review** and may squash-merge without a separate owner yes when everything is green and clear. A peer conflict, red CI or an unclear breaking change: do not merge; open or take a [backlog](../project.md#standing-backlog) item through the FSM, or decline the PR with a reason, or ask the owner. After every merge, notify the owner ([merge notice](../templates/owner/merge-notice.md)).
- **docker and github-actions** (including grouped updates that may contain majors): owner «да» before merge, because merge deploys to dev with SSH secrets and CI does not validate image or action supply chain the way app build and tests do.
- Merge order rules apply (one at a time, green Deploy between).
