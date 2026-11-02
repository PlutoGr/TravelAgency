# Templates
Every artifact is created from one of these files. Rules link here instead of inlining. Text inside the code blocks is copied; `<...>` are placeholders. GitHub and wiki templates are Russian and impersonal; agent templates are English; owner templates are Russian (owner chat).

## GitHub artifacts
| Template | Used for | Rules |
|---|---|---|
| [epic-issue](github/epic-issue.md) | epic issue | [epic](../elements/issues/epic.md) |
| [feature-issue](github/feature-issue.md) | feature issue | [feature](../elements/issues/feature.md) |
| [bug-feature-issue](github/bug-feature-issue.md) | bug-feature issue | [bug-feature](../elements/issues/bug-feature.md) |
| [non-product-issue](github/non-product-issue.md) | process, infra, question issue | [non-product work](../lifecycle/non-product-work.md) |
| [issue-comment](github/issue-comment.md) | any substantial issue or PR comment | [issues](../elements/issues/README.md) |
| [status-comments](github/status-comments.md) | take, stage advance, owner question, decision, hold | [columns](../elements/board/columns/README.md) |
| [acceptance-comment](github/acceptance-comment.md) | Приёмка result | [stage 11](../lifecycle/11-deploy-and-acceptance.md) |
| [commit](github/commit.md) | commit and squash title | [commits](../elements/git/commits.md) |
| [branch-name](github/branch-name.md) | branch | [branches](../elements/git/branches.md) |
| [pull-request](github/pull-request.md) | PR body | [pull requests](../elements/git/pull-requests.md) |
| [review-replies](github/review-replies.md) | replies on finding threads | [stage 10](../lifecycle/10-review-and-merge.md) |
| [review-verdict](github/review-verdict.md) | escalated review verdict | [stage 10](../lifecycle/10-review-and-merge.md) |

## Wiki pages
| Template | Used for | Rules |
|---|---|---|
| [epic-page](wiki/epic-page.md) | `Epic-<Name>` | [epic pages](../elements/wiki/epic-pages.md) |
| [feature-page](wiki/feature-page.md) | `Feature-<Name>` | [feature pages](../elements/wiki/feature-pages.md) |
| [doc-page](wiki/doc-page.md) | `Doc-<Name>` | [Doc- pages](../elements/wiki/doc-pages.md) |
| [adr-page](wiki/adr-page.md) | `Doc-ADR-NNN-<Name>` | [Doc- pages](../elements/wiki/doc-pages.md) |
| [plan-page](wiki/plan-page.md) | `Plan-Product-Vision`, `Plan-Roadmap` | [Plan- pages](../elements/wiki/plan-pages.md) |
| [decisions-table](wiki/decisions-table.md) | «Решения» section | [decisions log](../elements/wiki/decisions-log.md) |
| [sidebar](wiki/sidebar.md) | `_Sidebar.md` | [sidebar](../elements/wiki/sidebar.md) |
| [moved-page-stub](wiki/moved-page-stub.md) | renamed page stub | [wiki](../elements/wiki/README.md) |

## Cloud agents (English)
| Template | Used for | Rules |
|---|---|---|
| [role-brief](agents/role-brief.md) | structure of every role brief | [briefs](../briefs/README.md) |
| [launch-brief](agents/launch-brief.md) | every cloud launch prompt | [cloud agents](../elements/cloud-agents.md) |
| [question](agents/question.md) | agent → orchestrator question | [cloud agents](../elements/cloud-agents.md#during-the-run) |
| [answer](agents/answer.md) | orchestrator → agent answer | [cloud agents](../elements/cloud-agents.md#during-the-run) |
| [agent-report](agents/agent-report.md) | agent final report | [cloud agents](../elements/cloud-agents.md#during-the-run) |
| [deliverable-review](agents/deliverable-review.md) | orchestrator verdict on a deliverable | [cloud agents](../elements/cloud-agents.md#acceptance) |

## Owner (Russian chat)
| Template | Used for | Rules |
|---|---|---|
| [owner-question](owner/owner-question.md) | one decision with options | [orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first) |
| [escalation](owner/escalation.md) | blocked work, failures | [orchestrator §7](../orchestrator.md#7-never-stall) |
| [merge-notice](owner/merge-notice.md) | after a merge | [stage 10](../lifecycle/10-review-and-merge.md) |
| [epic-demo](owner/epic-demo.md) | epic acceptance | [stage 13](../lifecycle/13-epic-acceptance.md) |
| [weekly-audit-report](owner/weekly-audit-report.md) | weekly audit result | [weekly audit](../lifecycle/weekly-audit.md) |
