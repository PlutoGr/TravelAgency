# Pipeline note
Box file `/home/box/shared/travelagency/work/pipeline.md`: the single in-flight map of what the orchestrator waits on. Not in git.

- **Format:** a table `Issue | PR | Sha | Agent | Waiting on | Next | Notes` and an `Updated:` line (MSK). Issue, `Waiting on` and `Next` are always filled; PR, sha and agent id whenever they exist (sha = head sha before merge, merge sha after).
- **Write when:** an item starts waiting on CI, Deploy, a cloud agent or the owner (set `Waiting on` and `Next`); clear it when the wait ends.
- **Read when:** every wake, before taking new work. A CI/CD wake is matched to rows by sha ([idempotent wakes](../lifecycle/events.md#wake-sources)).
- A row is the recovery point after a lost context: with it and the issue comments the next step is clear without chat history.
- No per-task wait-watch routine and no arm-then-check; wakes come from [events](../lifecycle/events.md). Anti-stall rules: [orchestrator §7](../orchestrator.md#7-never-stall).
