# В работе (`статус: в работе`)
- **Applies to:** product epic only. Never set it on a feature; never restore a feature to it. Never the standing backlog (#8).
- **Enter:** when the first child feature leaves Очередь; also when the owner finds gaps at epic demo ([epic demo](../../../lifecycle/13-epic-acceptance.md)).
- **While here:** child features run their own columns. Keep the epic here while any child is open. Memory: `Waiting on` = `children` ([vocabulary](../../memory.md#waiting-on-values)); when every child is Готово and Status «Демо» is missing, `Question` = `create Status «Демо»` ([Question](../../memory.md#question)). The child-close step of the [close procedure](done.md) resumes it.
- **Leave:** a product epic → Демо when every listed child is Готово (owner demo), subject to the [Question guard](../../memory.md#question-guard). Issue **#8** stays `статус: очередь` for its whole life: taking or closing its children never moves #8; #8 is never Демо and is never closed because children finished.
- **Who moves:** orchestrator, in the same step as the take or close of a child (part of that step, never a separate later task). No script infers the epic label from children.
- **Required:** the `Epic-` page «Разбиение» lists every child.
