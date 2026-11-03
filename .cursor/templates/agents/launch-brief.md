# Launch brief (cloud agent prompt)
Every cloud launch prompt is filled from this template, in English. Keep the instructions short (about 15-30 lines); the Read list may be as long as the touchpoints need. Rules: [cloud agents](../../elements/cloud-agents.md).

```markdown
Role: read and follow `.cursor/briefs/<role>.md` (you are the <role> described there) and `.cursor/rules/00-core.mdc`, `.cursor/rules/security.mdc`.
Task: issue #<NN> «<title>», <Epic|Feature|bug-feature|non-product>, column «<column>».
Stage rules: `.cursor/lifecycle/<stage file>`.

Goal and acceptance:
- <what must be true when you stop; the exit gate of the stage>

Read:
- Wiki: <Epic-/Feature- page URL + section anchor>
- Current behavior: <Doc- pages this task touches>
- Decisions: <«Решения» rows or comments that already decided the approach>
- Architecture context: <parent epic architecture choices; project-wide constraints (services, contracts, auth)>
- Stack touchpoints: <only the services, files, tests this task hits>

Do:
- <concrete steps>

Boundaries:
- Everything in «Forbidden for every cloud agent» (`.cursor/elements/cloud-agents.md`) and in your role brief.
- Out of scope: <explicit list>

Deliverable:
- <code: PR from branch `<type/NN-slug>` per `.cursor/templates/github/pull-request.md`, ready only when the ready predicate in `.cursor/lifecycle/10-review-and-merge.md` holds>
- <text: ready Russian section text for «<section>» per `.cursor/templates/wiki/<page>.md`; no wiki push>

Questions: if something blocks a correct result, stop, ask with `.cursor/templates/agents/question.md`, and end the run with `RESULT blocked` (do not stay open waiting for an answer).
Report: `.cursor/templates/agents/agent-report.md`.
Token thrift: short brief only (this prompt). Minimal diff, no drive-by refactor, stop when acceptance holds. Report a real smell; do not hunt.
```
