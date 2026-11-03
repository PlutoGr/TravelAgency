# Briefs (role briefs for cloud agents)
A brief defines who a cloud agent is for one launch. It is not a separate bot. Every brief follows the [role brief structure](../templates/agents/role-brief.md); every launch prompt follows the [launch brief template](../templates/agents/launch-brief.md) and points to one brief. Which brief for which stage: [cloud agents](../elements/cloud-agents.md#which-brief).

| Brief | Role |
|---|---|
| [analyst.md](analyst.md) | senior product analyst: narrow helper for epic expectations or a thin feature confirm; not the default author |
| [architect.md](architect.md) | senior solution architect: epic Архитектура and Разбиение, feature Архитектура |
| [test-designer.md](test-designer.md) | senior QA engineer: Тест-кейсы |
| [developer.md](developer.md) | senior full-stack developer: application code and tests |
| [devops.md](devops.md) | senior DevOps engineer: CI, Docker, nginx, deploy files |
| [reviewer.md](reviewer.md) | senior code and security reviewer: escalated PR review |
| [docs-writer.md](docs-writer.md) | technical writer: Крупная `Doc-` and ADR text after merge |
| [methodology-auditor.md](methodology-auditor.md) | process auditor: `.cursor/` consistency and process simulation |

Common to all: the [core](../rules/00-core.mdc), [security](../rules/security.mdc), the forbidden list in [cloud agents](../elements/cloud-agents.md#forbidden-for-every-cloud-agent), [question](../templates/agents/question.md) and [report](../templates/agents/agent-report.md) templates. No brief allows talking to the owner, changing labels, the board or the wiki, or merging.
