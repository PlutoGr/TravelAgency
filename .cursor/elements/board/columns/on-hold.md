# Удержание (`статус: удержание`)
- **Applies to:** all. Not part of any sequence.
- **Enter:** the item is paused (owner decision or a dependency that will not clear soon), with the [hold comment](../../../templates/github/status-comments.md) saying why, what resumes it and which column to restore. Hold only by label, never by a body prefix ([board](../README.md)). An owner stop is recorded this way in the same turn.
- **Pausing an epic:** `статус: удержание` on the epic; its open children are paused too: Очередь children get «Приоритет» cleared and are not taken, an in-flight child is held at a safe point unless the owner says to finish it. The epic's hold comment lists the paused children. Nothing resumes without a recorded owner resume («**Итог:** решено: ...» on the epic).
- **Leave:** to the recorded column, or Очередь with «Приоритет»; an owner-paused item only after the recorded owner resume.
- **Who moves:** orchestrator.
