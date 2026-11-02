# Question (cloud agent → orchestrator)
Raised when a gap, contradiction or architecture limit blocks a correct result. Do not guess around it. Rules: [cloud agents](../../elements/cloud-agents.md#during-the-run).

Every question stops the work and ends the run with `RESULT blocked`, so the orchestrator wakes. There is no non-blocking question: a doubt that does not block goes into «Open doubts» of the [agent report](agent-report.md). The orchestrator answers in that wake ([answer](answer.md)) and resumes the agent with a reply to the same cloud agent; an owner-needed answer pauses the issue in place first.

```markdown
QUESTION <issue #NN> <column>
Context: <file, page section, line; what was being done>
Problem: <what is missing, contradictory or wrong; quote the page or code>
Options:
1. <option>: effect <...>
2. <option>: effect <...>
Recommendation: <option and why>
Proposed «Решения» row: <Вопрос | Варианты | (empty) | Почему | Дата>  (Russian, when the answer changes the page)
RESULT blocked
```
