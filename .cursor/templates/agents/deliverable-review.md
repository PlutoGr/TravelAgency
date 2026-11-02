# Deliverable review (orchestrator verdict)
The orchestrator's checklist on every cloud-agent deliverable. Kept in the orchestrator's working notes; its outcome is published as the stage comment ([status comments](../github/status-comments.md)) or a PR review. Accepted section text is published (wiki, or an issue comment when the page cannot be pushed yet) in the same turn it is accepted, never left only in the agent report. On an [owner stage approval](../../orchestrator.md#owner-stage-approval) column, accept publishes the section and Next is the owner digest; the stage-advance comment is posted only with the later label change. Rules: [cloud agents](../../elements/cloud-agents.md#acceptance).

```markdown
REVIEW <issue #NN> <column> <agent / PR / head sha>
Stage bar (from the lifecycle stage file): <each exit criterion: pass / fail>
Scope: <inside issue and epic problem: yes / no; drive-by changes: none / list>
Templates and formats: <pass / fixes needed>
Public wording and secrets: <pass / fixes needed>
Agent doubts and questions: <answered / open>
Verdict: accept | steer (reply to same agent: <ask>) | redo (new launch: <brief fix>) | dismiss (cancel and archive: <reason>)
Next: <label change, publish, owner stage approval, merge gate, or wait>
```
