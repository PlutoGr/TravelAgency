# Branches
Template: [branch name](../../templates/github/branch-name.md) `type/NN-short-english-slug`.

- ASCII lowercase English only (no Cyrillic, no spaces). `NN` = issue number as its own slash segment (required for issue↔branch matching). `short-english-slug` = 2-5 kebab-case words.
- `type`: `feature` | `fix` | `docs` | `infra` | `chore` | `deps`. From commit types: разработка→feature, исправление→fix, документация→docs, инфраструктура→infra, зависимости→deps, тесты and аналитика→chore (docs when the PR touches only wiki or rules).
- **Who creates:** the orchestrator before launch: `gh issue develop <NN> --name type/NN-short-english-slug --base main` (records `linkedBranches`; check with `gh issue develop --list <NN>`). If the branch already exists, do not add or remove Development links by hand.
- Dependabot keeps its default branch names.
- Prefixes other than `type/` (`lead/`, `cursor/`, `developer/`, Russian prefixes): replace with `type/`; prefer deleting old junk branches over renaming (deletion of unmerged branches needs the owner).
- After merge, head branches auto-delete (repo setting «Automatically delete head branches»; if off, ask the owner to enable it). Orphans are cleaned in the weekly audit.
