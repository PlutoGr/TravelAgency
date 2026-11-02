# В работе (`статус: в работе`)
- **Applies to:** epic only. Never set it on a feature; never restore a feature to it.
- **Enter:** when the first child feature leaves Очередь; also when the owner finds gaps at epic acceptance ([stage 13](../../../lifecycle/13-epic-acceptance.md)).
- **While here:** child features run their own columns. Keep the epic here while any child is open.
- **Leave:** a product epic → На ревью when every child is Готово. The standing bugs epic stays here while it has open children and returns to Очередь when it has none; it goes to На ревью only when the owner asks to close it.
- **Who moves:** orchestrator, in the same step as the take or close of a child (part of that step, never a separate later task; `board-sync` does not infer the epic label from children).
- **Required:** the `Epic-` page «Разбиение» lists every child.
