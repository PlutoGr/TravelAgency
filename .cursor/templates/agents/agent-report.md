# Agent report (final message of a cloud agent)
English. Rules: [cloud agents](../../elements/cloud-agents.md#during-the-run). A QUESTION turn does not post this report: its final message is the [question](question.md) block, whose only final line is `RESULT blocked`.

```markdown
RESULT <issue #NN> <column>: done | partial
Deliverable: <PR URL + head sha | section text below | review verdict>
What changed: <short list>
Tests: <added or updated; CI status>
Acceptance check: <each acceptance item: met / not met>
Open doubts: <non-blocking list or "none">  (a blocking doubt is a QUESTION instead of this report)
Doc- page: <Крупная: name to draft after merge | Малая: name, draft text is in the PR | «вики не затронута» | n/a>
<Section text (Russian), when the deliverable is text>
```
