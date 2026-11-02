# В работе (`статус: в работе`)
- **Applies to:** epic only. Never set it on a feature; never restore a feature to it.
- **Enter:** when the first child feature leaves Очередь; also when the owner finds gaps at epic acceptance ([stage 13](../../../lifecycle/13-epic-acceptance.md)).
- **While here:** child features run their own columns. Keep the epic here while any child is open.
- **Leave:** → На ревью when every child is Готово.
- **Who moves:** orchestrator, on take and close of a child (`board-sync` does not infer the epic label from children).
- **Required:** the `Epic-` page «Разбиение» lists every child.
