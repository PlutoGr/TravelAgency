# Weekly audit
Routine «Еженедельный аудит порядка», Monday 12:00 Europe/Moscow, by the orchestrator ([wake sources](events.md#wake-sources)). Goal: the process still delivers staged, tracked, closable work with order and throughput, and the project stays clean and secure. A repo-side consistency pass over `.cursor/` may be delegated to a cloud agent with the [methodology auditor brief](../briefs/methodology-auditor.md).

## Checklist
1. **Board:** every open issue has exactly one `статус:*`; closed issues only `статус: готово`; no feature on `статус: в работе`; epics in В работе only while children are in flight (the standing bugs epic with no open children sits in Очередь); Готово newest-first; Очередь top = current epic's children and the stated «Приоритет» ([board](../elements/board/README.md), [queue priority](../elements/board/queue-priority.md)).
2. **Tasks:** every open issue taken or edited since the last audit matches its template (others are brought to it when next taken); no subtask or mini-feature; bug-features link their source feature; no stale «На ревью» without a pending owner question; every Удержание has a hold comment that names what resumes it; every Очередь card has «Приоритет» (set missing ones).
3. **Git and PRs:** active remote branches match `type/NN-short-english-slug` ([branches](../elements/git/branches.md)); clean orphans that will not auto-delete (deleting branches other than merged head branches needs the owner); no open PR without an owner and next step; recent PRs have `#NN`, a «Документация» line, finished finding replies. Do not retrofit closedBy on old closed issues.
4. **Wiki:** no `Doc-` page edited before `main`; no status line treated as truth; sidebar complete; Plan- pages current ([wiki](../elements/wiki/README.md)).
5. **Automations:** every inventory row still Active (Bugbot scope = this repo only); spot-check 1-2 recent PRs; latest weekly vuln scan run history ([automations](../elements/automations.md)).
6. **Rules:** `.cursor/` consistent with practice; one way of doing each thing; templates match live artifacts.
7. **Pipeline note** current; no hanging cloud agent; stale waits re-checked (agent silence, Deploy catch-up, board-sync and notify runs failed since the last audit).
8. **Owner waits:** each blocking На ревью is listed once in the report with its original recommendation (not a new question).

## Output
Fix clear reversible drifts at once on work taken or edited since the last audit, and any wrong label, missing «Приоритет» or missing hold reason anywhere ([orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first)); older off-template items follow the [in-flight rule](README.md#gates). Send the owner a short [weekly audit report](../templates/owner/weekly-audit-report.md) only when something needs a decision or was fixed that the owner should know; propose rule changes there.
