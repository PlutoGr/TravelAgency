# Удержание (`статус: удержание`)
- **Applies to:** all. Not part of any product sequence. Used for a **long hold of the work stream** (owner stop of an item or epic), not for an ordinary owner question.
- **Enter:** with the [hold comment](../../../templates/github/status-comments.md) saying why, what resumes it and which column to restore. Hold only by label. Record the pause in [memory](../../memory.md) in the same turn.
- **Pausing an epic:** `статус: удержание` on the epic; clear «Приоритет» on its Очередь children; cancel the in-flight child's cloud agent; move that child to Удержание with the restore column named in its hold comment. The epic's hold comment lists the paused children. Nothing resumes without a recorded owner resume.
- **Owner question during a stage:** do **not** use this column. Keep the issue in its current column, record `Waiting on` = owner in memory, ask the owner ([silence policy](../../../orchestrator.md#8-never-stall)).
- **Leave:** to the recorded column, or Очередь with «Приоритет»; an owner-paused item only after the recorded owner resume.
- **Who moves:** orchestrator.
