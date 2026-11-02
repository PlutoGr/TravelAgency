# Queue priority (field «Приоритет»)
Canonical method for ranking Очередь (1 = next). Set only by the orchestrator. No script invents, clears or moves «Приоритет» (board hygiene deleted / orchestrator-only).

## Layers
1. Roadmap (`Plan-Roadmap`) sets the coarse sequence ([roadmap](../roadmap.md)).
2. Epic sequence or owner-confirmed parallel sets follow the roadmap and `Epic-` «Связи с другими эпиками» ([epic pick](../../lifecycle/02-roadmap.md)).
3. «Приоритет» on Очередь cards ranks concrete tasks.

## Current epic first
The current product epic is the one the owner confirmed at epic pick ([02](../../lifecycle/02-roadmap.md)); with a confirmed parallel set, each epic of the set. The [standing backlog](../../project.md#standing-backlog) (#8) is never the current product epic. An epic in Демо (owner demo) generates no new queue numbers except for the gap features just sent back to Очередь. A paused epic (Удержание) generates none: its children have no «Приоритет» until the recorded owner resume ([Удержание](columns/on-hold.md)). Its open Очередь children get contiguous low numbers, ordered by dependency and delivery logic; a confirmed parallel set is interleaved the same way; all ahead of backlog children. Dependabot and non-product issues already in flight are not the next roadmap epic. The owner may set an explicit order; that is an instruction, not a standing rule that the backlog outranks the current epic.

## Outside the epic order
- Non-product work (Dependabot, infra, process docs) follows «Приоритет» without an epic pick.
- Red CI or a failed Deploy on `main`, and a validated security vulnerability, are taken promptly through the FSM (thin stages OK) via the [standing backlog](../../project.md#standing-backlog) or the current epic when they belong there ([bugs](../../lifecycle/bugs.md)), without jumping product epics out of order.

## When to refresh
1. On every new issue filed or accepted: set «Приоритет» at once; adjust neighbours only as needed, not a full reshuffle. Every Очередь card except paused-epic children has a number; the weekly audit sets missing ones, so an old card without a number never outranks new work by issue number alone.
2. After an epic pick: set epic and child numbers.
3. On demand when the owner asks (including a one-shot catch-up).
4. Weekly audit: Готово newest-first, Очередь top correct; fix cheap mismatches.

## Fallback sort
After explicit «Приоритет»: other Очередь cards by issue number. Unprioritized bugs do not outrank other unprioritized cards. `статус: удержание` is the Удержание column, not the bottom of Очередь.
