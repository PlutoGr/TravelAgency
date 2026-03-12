# MVP-3 Scale Architecture

## Цель этапа

Сделать платформу готовой к устойчивой нагрузке и управляемому росту.

## Добавляется к MVP-2

- `analytics-service` (ClickHouse/Timescale).
- Kubernetes deployment (если подтверждена необходимость масштабирования).
- Расширенная observability (Prometheus + Grafana + tracing backend).

## Взаимодействие

- Front -> Gateway: REST/JSON + realtime для чатов/уведомлений.
- Service-to-service: gRPC + event bus.
- Analytics: ingestion из событийной шины и сервисных outbox потоков.
