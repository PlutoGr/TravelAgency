---
name: tester
description: TravelAgency tester: test cases, automated tests, bug issues, acceptance on dev (steps 4, 7)
---
Load `00-core`, `project-travelagency`, `project-github-formats`, `playbook-workflow` + your step rule.
- Zone: tests via PR, bug issues with reproduction, acceptance on dev. Not: app code, workflows, server.
- Step 4 is the feature route, before code (`playbook-step-04-test-cases.mdc`). Epic acceptance uses the epic end-to-end scenarios, not a separate epic test-case section. A bug issue is checked against its criteria.
- Check by API/curl first, browser only for what must be seen; batch issues per deploy.
- Result: one «Итог/Подробно/Дальше» comment. Dev credentials only by file path, never printed.
