# Project: TravelAgency
Personal pet project: a travel agency website (search and compare package tours, booking request, manager support in chat) and its backend.

## Facts
- Repo `PlutoGr/TravelAgency` (public). Wiki: separate repo `TravelAgency.wiki.git`, branch `master`, push = publish (public).
- Board: one private GitHub Project titled «Задачи», at `PlutoGr/projects/2` ([board](elements/board/README.md)). If the live title differs, the orchestrator renames it to «Задачи».
- Stack: .NET 10 microservices (`src/Services`) behind a YARP gateway, gRPC between services, PostgreSQL, Redis, MinIO; React 19 + TypeScript + Vite frontend (`src/Frontend/travel-agency-frontend`, vitest). Tests: `*.UnitTests`, `*.IntegrationTests` (Testcontainers). Follow neighbouring code.
- Environments: one today, dev (Staging), behind basic auth with a self-signed cert. Test and prod come later; when they exist, add them here and in [deploy and acceptance](lifecycle/11-deploy-and-acceptance.md). Address and credentials are not in the repo; they are in the box-only ops file held by the orchestrator. Smoke on dev = login, catalog, booking.
- Merge to `main` = automatic deploy to dev («Deploy to dev»). CI checks: [CI/CD](elements/ci-cd.md).
- Repo wins over any description; [commit](templates/github/commit.md) and [PR](templates/github/pull-request.md) templates (mirrored by `.github/pull_request_template.md`) win on format conflicts.
- Team rules (secrets, merge order and the rest) live in this package, never in the wiki. Private ops facts are in `/home/box/shared/travelagency/OPS.md`.

## Standing backlog
One backlog issue for accidental bugs and tiny features that do not belong to a product epic: **#8** («мелкие доработки»). Rename and reformat that issue in the later docs-and-tasks phase. Items go there first; when taken, they may become epics on the roadmap. Children use the normal Feature / bug-feature FSM ([bugs](lifecycle/bugs.md)).

## Product themes
Coarse product focus (not the «Приоритет» field): 1. Bugs and UX. 2. Profiles. 3. Chat. 4. Aggregation. The orchestrator orders work within this; changing the theme order needs the owner. Queue numbers: [queue priority](elements/board/queue-priority.md).

## Parallel work
- A **stream** is one feature (one issue, one agent, one pull request).
- Features under one epic may run in parallel. Merge one PR at a time; per stream 1 active item and at most 1 waiting.
- Start the next item only if it touches no files, endpoints or migrations of an open PR, does not depend on an unmerged PR, and its criteria are final. Branch from fresh `main`.
- Any doubt about overlap: wait, or launch a review with the [reviewer brief](briefs/reviewer.md). After the previous merge, update the branch before review.
- Returns first (acceptance miss, review changes, new analysis for an item in flight) over new work.
- Temporary dumps on dev are allowed if deleted after the check.
