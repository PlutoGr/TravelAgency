# MVP-2 Security and Reliability

## Security additions

- Подпись/валидация сообщений в брокере через доверенный transport.
- Ограничение прав producer/consumer по очередям.
- Retention и masking политики для payload уведомлений.

## Reliability additions

- Dead-letter queue для непроцессируемых сообщений.
- Retry policy consumers с backoff.
- Мониторинг lag очередей и количества retry.
