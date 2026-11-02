# 09 Development (column Разработка)
**Entry:** «Полное ТЗ» holds ([08](08-test-cases.md)). Do not enter, and do not keep coding, while it fails.

**Who:** one cloud agent and one PR: [developer brief](../briefs/developer.md) for application code and its tests; [devops brief](../briefs/devops.md) for workflows, Docker, nginx and deploy files. Launch per [cloud agents](../elements/cloud-agents.md) with the [launch brief](../templates/agents/launch-brief.md).

**Rules**
- Branch `type/NN-short-english-slug` created by the orchestrator with `gh issue develop` when it does not exist ([branches](../elements/git/branches.md)).
- Write the marked automated tests first, or in the same TDD cycle, then the code. Implement «Архитектура». Add no field, control or block Аналитика did not justify. Stay on the epic's problem.
- A typo, or a 500 caused by a typo, inside this feature is fixed here; no new issue ([bugs](bugs.md)).
- Deviation needed: stop and raise a [question](../templates/agents/question.md) with a proposed «Решения» row. Do not code around the page. If the architecture prevents a clean, secure solution, raise an architecture-change question instead of hacking.
- PR per [pull requests](../elements/git/pull-requests.md): «Задача» `#NN` only, never `closes`/`fix`/`fixes`; «Документация» names the `Doc-` page to write after merge or «вики не затронута»; no wiki edit in the PR. Commits per [commits](../elements/git/commits.md).
- The agent marks the PR ready when CI is green and reports with the [agent report](../templates/agents/agent-report.md).

**Output:** a ready PR with green CI. The column stays Разработка through review, merge and the wait for Deploy ([10](10-review-and-merge.md), [11](11-deploy-and-acceptance.md)).
