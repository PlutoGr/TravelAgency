# Git
- Follow the repo's architecture and neighbouring code.
- Minimal diff, implement to the acceptance criteria. No libraries "just in case", no refactoring of neighbouring layers unless asked.
- Never push to `main`, never force-push; history rewrite only with the owner's yes.
- Leave shared clones clean (default branch, no uncommitted changes).
- Locally run only tests for the touched area; CI is the gate (do not re-run locally what CI ran green; read failures from the job log, quote only failing lines). Full regression only before a release or after wide changes (auth, routing, migrations).

Specific: [branches](branches.md), [commits](commits.md), [pull requests](pull-requests.md).
