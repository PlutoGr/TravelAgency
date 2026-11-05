# Cloud agents
Cursor cloud agents do the repo-bound stages that need them: epic architecture (including a gap found on a child), a large split, test cases when the policy says so, code and tests, CI and Docker edits, escalated review, a `Doc-` draft when the skeleton must change, rules audits, and a narrow analysis helper. The orchestrator writes initial wishes, runs live epic expectations with the owner, the thin feature TZ, a simple split, and the test-policy line. Launch prompts stay short: role, goal, links, bar. Do not paste the methodology into a launch. The orchestrator itself also does what needs the server, secrets, chat history, merge, board labels, wiki publish, or acceptance on dev. Never both on the same artifact. Cloud-first under the unified subscription (2026-10-28): no usage-mode matrix, no scheduled limits refresh.

## Which brief
| Stage or job | Brief |
|---|---|
| Epic Бизнес-анализ helper (peer notes, mock scaffold); a short note on a feature TZ only when asked | [analyst](../briefs/analyst.md) |
| Epic Архитектура, including a gap found on a child; epic Разбиение when large or tangled | [architect](../briefs/architect.md) |
| Тест-кейсы (Крупная and `Тесты: сначала сценарии`, or an item already in that column) | [test designer](../briefs/test-designer.md) |
| Application code and its automated tests (Разработка) | [developer](../briefs/developer.md) |
| Workflows, Docker, nginx, deploy files | [devops](../briefs/devops.md) |
| Escalated PR review, overlap check | [reviewer](../briefs/reviewer.md) |
| Крупная `Doc-` and ADR text after merge | [docs writer](../briefs/docs-writer.md) |
| `.cursor/` consistency audit, process simulation | [methodology auditor](../briefs/methodology-auditor.md) |

Acceptance on dev is not a cloud launch: the orchestrator has dev access ([Приёмка](../lifecycle/11-deploy-and-acceptance.md)).

## Launch
1. Check that no branch, PR or agent already exists for the issue (`gh pr list`, `git ls-remote`). One issue, one agent: a live run on the issue (for example a `Doc-` draft during Приёмка) is finished or cancelled before another is launched ([row lifecycle](memory.md#row-lifecycle)).
2. Create the branch for code work ([branches](git/branches.md)).
3. Repo `https://github.com/PlutoGr/TravelAgency`, title with the issue number.
4. Prompt in English from the [launch brief template](../templates/agents/launch-brief.md): role brief link, issue, column, goal and acceptance, page links, architecture context, minimal stack touchpoints, boundaries, deliverable, report format. The agent reads the `.cursor/` files the brief links. Do not paste the rule set, the lifecycle, or unrelated guides.
5. Record the run id in the row's `Agent` field in [memory](memory.md) by the [row lifecycle](memory.md#row-lifecycle) (`Waiting on` = `agent` only when the agent is the wait).

## Models
- Default Grok, medium effort. Low for mechanical edits; high for multi-file work, auth or security; extra-high for hard work, including a review diff of about 150 lines or more.
- Opus only if Grok at extra-high failed, or the task is large and critical (architecture, security, data migration); state why. 1m context or max effort never without the owner's yes.

## Forbidden for every cloud agent
- Push to `main`, merge, self-accept on dev, mark its own stage done on the board.
- Edit the wiki, the board, a label or a Projects field.
- Secrets or server access.
- `closes`/`fix`/`fixes`, `addCloseIssueReferences`, closedBy.
- Leave the issue scope or the epic's «Ожидания».
- Talk to the owner.
- Open anything other than a regular PR into `main` when the result is code.

## During the run
- A blocking doubt is raised to the orchestrator with the [question template](../templates/agents/question.md) (in the PR, the report, or the agent thread) and the run ends with `RESULT blocked` so the orchestrator wakes. Do not leave the run open waiting for an answer. Every question blocks; there is no «continue meanwhile». The orchestrator answers in that wake with the [answer template](../templates/agents/answer.md) as a reply to the same cloud agent, which resumes the run (owner-needed: pause in place with the `Question` and the blocked agent id per the [row lifecycle](memory.md#row-lifecycle), then the same reply once the owner answers). The agent does not guess around a gap in the page.
- A real defect, risk or smell outside the task is named in the report under Smells. Do not hunt for smells and do not invent them. Do not silently expand the task to fix one.
- Text deliverables (architecture, a large split, test cases, a narrow analysis helper, `Doc-`) are returned as ready Russian section text, not a wiki push.
- **Code deliverables (Разработка):** the agent opens the PR, watches CI, Secret scan and Cursor Automations at the [depth of the feature](automations.md#depth-by-size), fixes red CI that belongs to the PR, answers or marks every bot finding, and only when the [ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate) holds marks the PR ready and reports. Until that report, the agent owns the PR; the orchestrator does not merge early.
- Final report: [agent report](../templates/agents/agent-report.md) (for code: state each part of the [ready predicate](../lifecycle/10-review-and-merge.md#ready-predicate)).

## Acceptance
The orchestrator owns the result as if written by hand: [deliverable review](../templates/agents/deliverable-review.md) → accept, steer (`reply` to the same agent with a concrete fix ask; in the same turn set `Agent` to the new run, `Waiting on` = `agent` only when the agent is the wait (otherwise unchanged), `Next` = wait for the new report), redo (cancel, fix the brief, relaunch), or dismiss (cancel and archive). For a PR: **full review** after the agent reports ready ([review and merge](../lifecycle/10-review-and-merge.md)). Accepted section text is published in the same turn. On an [owner stage approval](../orchestrator.md#owner-stage-approval) column the label stays until the owner's yes. Do not leave an orphan agent or PR. Failures and timeouts: [never stall](../orchestrator.md#8-never-stall).
