# Pipeline note
Box file `/home/box/shared/travelagency/work/pipeline.md`: the single in-flight map of what the orchestrator waits on. Not in git.

- **Format:** a table `Waiting on | Next | Notes` and an `Updated:` line (MSK).
- **Write when:** an item starts waiting on CI, Deploy, a cloud agent or the owner (set `Waiting on` and `Next`); clear it when the wait ends.
- **Read when:** every wake, before taking new work.
- No per-task wait-watch routine and no arm-then-check; wakes come from [events](../lifecycle/events.md). Anti-stall rules: [orchestrator §7](../orchestrator.md#7-never-stall).
