# Dependencies (Dependabot)
Dependabot PRs have no issue and keep default branch names; commit prefixes per [commits](git/commits.md).

- **npm and NuGet** (minor, patch and major; majors are separate PRs, groups are minor/patch only): squash-merge without a separate owner yes when CI is green, automations finished, findings answered, and nothing needs clarification. A peer conflict, red CI or an unclear breaking change needs clarification: do not merge; notify the owner or follow the fix path (a briefed fix PR). After every merge, notify the owner what was merged ([merge notice](../templates/owner/merge-notice.md)).
- **docker and github-actions** (including grouped updates that may contain majors): owner «да» before merge, because merge deploys to dev with SSH secrets and CI does not validate image or action supply chain the way app build and tests do.
- Merge order rules apply (one at a time, green Deploy between).
