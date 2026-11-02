# Queue priority (field «Приоритет»)
Canonical method for ranking Очередь (1 = next). Set only by the orchestrator. `board-sync` never invents numbers: it sorts by the field, clears it when a card leaves Очередь, and orders Готово by newest closed.

## Layers
1. Roadmap (`Plan-Roadmap`) sets the coarse sequence ([roadmap](../roadmap.md)).
2. Epic sequence or owner-confirmed parallel sets follow the roadmap and `Epic-` «Связи с другими эпиками» ([epic pick](../../lifecycle/02-roadmap.md)).
3. «Приоритет» on Очередь cards ranks concrete tasks.

## Current epic first
The current product epic is the one whose label is a column other than Очередь, Готово or Удержание. Its open Очередь children get contiguous low numbers, ordered by dependency and delivery logic; a confirmed parallel set is interleaved the same way; all ahead of the standing bugs epic and the backlog. Dependabot and non-product issues already in flight are not the next roadmap epic. The owner may set an explicit order; that is an instruction, not a standing rule that the bugs epic outranks the current epic.

## When to refresh
1. On every new issue filed or accepted: set «Приоритет» at once; adjust neighbours only as needed, not a full reshuffle.
2. After an epic pick: set epic and child numbers.
3. On demand when the owner asks (including a one-shot catch-up).
4. Weekly audit: Готово newest-first, Очередь top correct; fix cheap mismatches.

## Fallback sort
After explicit «Приоритет»: other Очередь cards by issue number. Unprioritized bugs do not outrank other unprioritized cards. `статус: удержание` is the Удержание column, not the bottom of Очередь.
