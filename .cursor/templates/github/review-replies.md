# Review replies
One reply on each open finding thread before merge, then resolve the thread when the UI allows. Rules: [cloud agent until ready](../../lifecycle/10-review-and-merge.md#1-cloud-agent-until-ready).

| Situation | Reply |
|---|---|
| Not applicable | `неактуально: <причина>` |
| Accepted, fix pending (not a finished state) | `принято, будет исправлено` |
| Fixed (also when a newer commit already fixed it) | `исправлено в <sha>` |
| Needs the owner (blocks merge; issue stays in column, `Question` in memory) | `нужно решение владельца: <вопрос>` |

A new PR or issue comment uses [issue comment](issue-comment.md).
