# 02 Roadmap upkeep and epic pick
Keep `Plan-Roadmap` true and choose what comes next. Element rules: [roadmap](../elements/roadmap.md), [queue priority](../elements/board/queue-priority.md).

**Entry:** an epic reached Готово; or before the first take from an epic the owner has not chosen; or intake changed the plan; or the weekly audit found the roadmap stale.

**Who:** the orchestrator proposes, the owner confirms.

**Steps**
1. Re-read `Plan-Roadmap`, `Plan-Product-Vision`, the candidate epics' «Архитектура», «Связи с другими эпиками» and related `Doc-` pages, so the next set stays coherent (a consistency check, not a rewrite).
2. Propose the next epic, or a parallel set when dependency or efficiency warrants it ([owner question](../templates/owner/owner-question.md)).
3. After the owner confirms: set epic «Приоритет» to the agreed sequence; raise that epic's open Очередь children to contiguous low numbers; for a confirmed parallel set interleave their children, still ahead of the standing bugs epic and the backlog.
4. Update `Plan-Roadmap` («Сейчас», open epics) when reality changed ([plan pages](../elements/wiki/plan-pages.md)).

**Output:** confirmed next epic or set, priorities set, roadmap page current.

**Exit gate:** owner confirmation recorded; the top of Очередь is the chosen epic's work. Do not open the next roadmap epic from «Приоритет» alone. The owner may override any order explicitly; that instruction is not a standing rule.
