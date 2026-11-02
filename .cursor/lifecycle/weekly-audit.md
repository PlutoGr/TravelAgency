# Weekly audit
Monday about 12:00 MSK, by the orchestrator. Goal: the process still delivers staged, tracked, closable work with order and throughput, and the project stays clean and secure. A repo-side consistency pass over `.cursor/` may be delegated to a cloud agent with the [methodology auditor brief](../briefs/methodology-auditor.md).

## Checklist
1. **Board:** every open issue has exactly one `статус:*`; closed issues only `статус: готово`; no feature on `статус: в работе`; epics in В работе only while children are in flight; Готово newest-first; Очередь top = current epic's children and the stated «Приоритет» ([board](../elements/board/README.md), [queue priority](../elements/board/queue-priority.md)).
2. **Tasks:** sample open issues against their templates; no subtask or mini-feature; bug-features link their source feature; no stale «На ревью» without a pending owner question.
3. **Git and PRs:** active remote branches match `type/NN-short-english-slug` ([branches](../elements/git/branches.md)); clean orphans that will not auto-delete (deleting branches other than merged head branches needs the owner); no open PR without an owner and next step; recent PRs have `#NN`, a «Документация» line, finished finding replies. Do not retrofit closedBy on old closed issues.
4. **Wiki:** no `Doc-` page edited before `main`; no status line treated as truth; sidebar complete; Plan- pages current ([wiki](../elements/wiki/README.md)).
5. **Automations:** every inventory row still Active (Bugbot scope = this repo only); spot-check 1-2 recent PRs; latest weekly vuln scan run history ([automations](../elements/automations.md)).
6. **Rules:** `.cursor/` consistent with practice; one way of doing each thing; templates match live artifacts.
7. **Pipeline note** current; no hanging cloud agent.

## Output
Fix clear reversible drifts on open work at once ([orchestrator §5](../orchestrator.md#5-powers-act-and-report-vs-ask-first)). Send the owner a short [weekly audit report](../templates/owner/weekly-audit-report.md) only when something needs a decision or was fixed that the owner should know; propose rule changes there.
