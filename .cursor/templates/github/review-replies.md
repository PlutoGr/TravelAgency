# Review replies
One reply on each open finding thread before merge, then resolve the thread when the UI allows. Rules: [stage 10](../../lifecycle/10-review-and-merge.md#3-findings-reply-before-merge).

| Situation | Reply |
|---|---|
| Not applicable | `неактуально: <причина>` |
| Accepted, fix pending (not a finished state) | `принято, будет исправлено` |
| Fixed (also when a newer commit already fixed it) | `исправлено в <sha>` |
| Needs the owner (blocks merge, issue to На ревью) | `нужно решение владельца: <вопрос>` |

A new PR or issue comment uses [issue comment](issue-comment.md).
