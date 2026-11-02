# AGENTS.md
Rules for agents live in `.cursor/`: start with `.cursor/README.md` and `.cursor/rules/00-core.mdc`. The controller is `.cursor/orchestrator.md`; cloud agents work from a role brief in `.cursor/briefs/`.
Lifecycle stages: `.cursor/lifecycle/`. Elements (board, issues, labels, git, PRs, CI/CD, wiki): `.cursor/elements/`. Templates for every artifact: `.cursor/templates/`.
Formats for commits and PRs: `.cursor/templates/github/commit.md`, `.cursor/templates/github/pull-request.md` (mirrored by `.github/pull_request_template.md`). Branch naming: `.cursor/elements/git/branches.md`.
Board stage = the issue `статус:*` label: `.cursor/elements/board/README.md`. Queue priority: `.cursor/elements/board/queue-priority.md`.
