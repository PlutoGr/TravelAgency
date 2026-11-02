# Cloud agents
Cursor cloud agents do every repo-bound methodology stage: analysis text, architecture text, split, test cases, code and tests, CI and Docker edits, escalated review, `Doc-` drafts, rules audits. The orchestrator chat stays thin: full brief in, PR or short report out. The orchestrator itself does what needs the server, secrets, chat history, live owner talk, merge, board labels, wiki publish, or acceptance on dev. Never both on the same artifact. Cloud-first under the unified subscription (2026-10-28): no usage-mode matrix, no scheduled limits refresh.

## Which brief
| Stage or job | Brief |
|---|---|
| Epic Бизнес-анализ, feature Аналитика | [analyst](../briefs/analyst.md) |
| Epic Архитектура, epic Разбиение, feature Архитектура | [architect](../briefs/architect.md) |
| Тест-кейсы | [test designer](../briefs/test-designer.md) |
| Application code and its automated tests (Разработка) | [developer](../briefs/developer.md) |
| Workflows, Docker, nginx, deploy files | [devops](../briefs/devops.md) |
| Escalated PR review, overlap check | [reviewer](../briefs/reviewer.md) |
| `Doc-` and ADR text after merge | [docs writer](../briefs/docs-writer.md) |
| `.cursor/` consistency audit, process simulation | [methodology auditor](../briefs/methodology-auditor.md) |

Acceptance on dev is not a cloud launch: the orchestrator has dev access ([stage 11](../lifecycle/11-deploy-and-acceptance.md)).

## Launch
1. Check that no branch, PR or agent already exists for the issue (`gh pr list`, `git ls-remote`). One issue, one agent.
2. Create the branch for code work ([branches](git/branches.md)).
3. Repo `https://github.com/PlutoGr/TravelAgency`, title with the issue number.
4. Prompt in English from the [launch brief template](../templates/agents/launch-brief.md): role brief link, issue, column, goal and acceptance, page links, architecture context, minimal stack touchpoints, boundaries, deliverable, report format. The agent reads this repo's `.cursor/` files the brief links; do not paste the whole rule set or unrelated guides.
5. Record the agent in [memory](memory.md) (`Waiting on` = agent).

## Models
- Default Grok, medium effort. Low for mechanical edits; high for multi-file work, auth or security; extra-high for hard work, including a review diff of about 150 lines or more.
- Opus only if Grok at extra-high failed, or the task is large and critical (architecture, security, data migration); state why. 1m context or max effort never without the owner's yes.

## Forbidden for every cloud agent
- Push to `main`, merge, self-accept on dev, mark its own stage done on the board.
- Edit the wiki, the board, a label or a Projects field.
- Secrets or server access.
- `closes`/`fix`/`fixes`, `addCloseIssueReferences`, closedBy.
- Leave the issue scope or the epic's problem.
- Talk to the owner.
- Open anything other than a regular PR into `main` when the result is code.

## During the run
- A blocking doubt is raised to the orchestrator with the [question template](../templates/agents/question.md) (in the PR, the report, or the agent thread) and the run ends with `RESULT blocked` so the orchestrator wakes. Do not leave the run open waiting for an answer. Every question blocks; there is no «continue meanwhile». The orchestrator answers in that wake with the [answer template](../templates/agents/answer.md) as a reply to the same cloud agent, which resumes the run (owner-needed: pause in place, then the same reply once the owner answers). The agent does not guess around a gap in the page.
- Text deliverables (analysis, architecture, split, test cases, `Doc-`) are returned as ready Russian section text, not a wiki push.
- **Code deliverables (Разработка):** the agent opens the PR, watches CI and Cursor Automations, fixes red CI that belongs to the PR, answers or marks every bot finding, and only then marks the PR ready and reports. Until that report, the agent owns the PR; the orchestrator does not merge early.
- Final report: [agent report](../templates/agents/agent-report.md) (for code: state that CI, Secret scan and Automations are finished and findings are answered).

## Acceptance
The orchestrator owns the result as if written by hand: [deliverable review](../templates/agents/deliverable-review.md) → accept, steer (`reply` to the same agent with a concrete fix ask and wait for the new ready report), redo (cancel, fix the brief, relaunch), or dismiss (cancel and archive). For a PR: **full review** after the agent reports ready ([stage 10](../lifecycle/10-review-and-merge.md)). Accepted section text is published in the same turn. Do not leave an orphan agent or PR. Failures and timeouts: [never stall](../orchestrator.md#8-never-stall).
