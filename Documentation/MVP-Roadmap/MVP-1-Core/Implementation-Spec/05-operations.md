# MVP-1 Operations

## Runtime

- Docker Compose как целевая среда этапа.
- Сервисы: gateway, identity, catalog, booking (+ chat/media опционально).

## Data ops

- Миграции отдельно в каждом сервисе.
- Ежедневный backup PostgreSQL.
- Проверяем восстановление минимум раз в месяц.

## Observability minimum

- Structured logs с `correlationId`.
- Health endpoints: `/health/live`, `/health/ready`.
- Базовые метрики: p95 latency, error rate, outbox queue length.
