# Dependencies (Dependabot)
Dependabot PRs have no issue and keep default branch names; commit prefixes per [commits](git/commits.md).

- A green safety-net CI wake on a `dependabot/*` branch wakes the orchestrator ([events](../lifecycle/events.md)). Red CI follows the unowned-red path (backlog item, [bugs](../lifecycle/bugs.md)); the PR gets a review row only once its CI is green. Record the PR in [memory](memory.md#waiting-on-values) with the Dependabot row shape.
- The CI wake **stays in the turn** and polls Automations for up to 15 minutes. Done in time: switch the row to `Waiting on` = `none`, `Next` = full review, and review in that turn.
- Not done in time: leave the row as `CI+Automations`. On the next weekday memory-refresh, Monday audit, or any later CI wake for that PR, check Automations again; once done, switch to `Waiting on` = `none`, `Next` = full review, and review in that wake. **Never merge before that switch.**
- After merge: `Waiting on` = `Deploy` with the merge in the [Sha cell](memory.md#sha-cell); the [row lifecycle](memory.md#row-lifecycle) deletes the row on its green Deploy.
- **npm and NuGet** (minor, patch and major; majors are separate PRs, groups are minor/patch only): after CI and Automations finish, the orchestrator does a **full review** and may squash-merge without a separate owner yes when everything is green and clear. A peer conflict, red CI or an unclear breaking change: do not merge; open or take a [backlog](../project.md#standing-backlog) item through the FSM, or decline the PR with a reason, or ask the owner. After every merge, notify the owner ([merge notice](../templates/owner/merge-notice.md)).
- **docker and github-actions** (including grouped updates that may contain majors): owner «да» before merge, because merge deploys to dev with SSH secrets and CI does not validate image or action supply chain the way app build and tests do.
- Merge order rules apply (one at a time, green Deploy between).
