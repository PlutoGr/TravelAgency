# Wiki
Separate public repo `TravelAgency.wiki.git`, branch `master`; push = publish. Project documentation only; team rules live in this package, never in the wiki.

## Structure
| Page kind | Holds | Rules | Template |
|---|---|---|---|
| `Home` | what the product is, what works now, sections | [home.md](home.md) | none (single page) |
| `_Sidebar`, `_Footer` | navigation | [sidebar.md](sidebar.md) | [sidebar](../../templates/wiki/sidebar.md) |
| `Doc-<Name>` | only what works in `main`; `Doc-ADR-NNN-<Name>` for decisions | [doc-pages.md](doc-pages.md) | [doc page](../../templates/wiki/doc-page.md), [ADR](../../templates/wiki/adr-page.md) |
| `Plan-<Name>` | only the current product vision and roadmap | [plan-pages.md](plan-pages.md) | [plan page](../../templates/wiki/plan-page.md) |
| `Epic-<Name>` | epic work in progress (sections per column) | [epic-pages.md](epic-pages.md) | [epic page](../../templates/wiki/epic-page.md) |
| `Feature-<Name>` | feature and bug-feature work in progress | [feature-pages.md](feature-pages.md) | [feature page](../../templates/wiki/feature-page.md) |
| «Решения» (section) | decision log on Epic and Feature pages | [decisions-log.md](decisions-log.md) | [decisions table](../../templates/wiki/decisions-table.md) |

No other page kinds. Sidebar groups: «Действующая документация» (Doc-), «Фичи» (Epic- and Feature-), «Планы» (Plan-).

## Who writes
Only the orchestrator publishes. Cloud agents return text: [analyst](../../briefs/analyst.md) (analysis sections), [architect](../../briefs/architect.md) (architecture, split, «Решения» rows; on Малая also «Тест-кейсы»), [test designer](../../briefs/test-designer.md) (Крупная «Тест-кейсы»), [docs writer](../../briefs/docs-writer.md) (Крупная `Doc-` and `Doc-ADR-NNN-<Name>` after the change ships). A Малая `Doc-` draft arrives in the PR and is published right after merge. Other text arrives via the issue or the PR.

## Writing
- Russian and impersonal ([templates](../../templates/README.md#russian-and-impersonal)); no customer company names.
- Simple example first (a request and response, a user step), then details. Public sources (for example Tourvisor) may be cited. Links to `#81`, `PR #82` are welcome.
- Code names (classes, fields, routes, commands) as in code. Dates `2026-10-17`. No «Обновлено» line (history shows dates).
- File names Latin, English, words joined by hyphens: `Doc-Booking-Request.md` (Cyrillic breaks links). The Russian title is only the first line `# <русский заголовок>`. Links without `.md`: `[text](Doc-Booking-Request)`, anchors `[text](Doc-Booking-Request#section)`.
- No status line as the source of truth: the stage is the issue label. «сделано» or «отменено» are content marks.
- Page templates live only in this package ([templates/wiki](../../templates/README.md#wiki-pages)); the wiki holds no template pages. An open page that does not match its current template is brought to it when next edited (drop a status line, drop epic «Тест-кейсы», roadmap order only in «Порядок»); closed pages are left alone.

## Lifecycle
- Shipped: `Doc-` (and ADR) published after merge, never from unmerged code ([Doc- pages](doc-pages.md)); the source page marked «сделано» with links. Cancelled: «отменено» with the reason.
- `Doc-` vs code mismatch: fix the wiki if the code is right, else open an issue.
- Rename: no redirects; update every link in the same commit; if issues or external sites link the old name, leave a one-line stub ([moved page stub](../../templates/wiki/moved-page-stub.md)).

## Push checklist
```bash
git pull --rebase                      # never force-push
# 1. _Sidebar.md updated for every new, renamed or deleted page
# 2. every [text](Name) link resolves to an existing file
gitleaks dir --redact . && gitleaks git --redact .
```
Commit `документация(вики): <что изменено>` with «Зачем» and «Как работает» ([commit](../../templates/github/commit.md)).
