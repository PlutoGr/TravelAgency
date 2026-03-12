# MVP-2 API Contracts

## Внешний API

- Сохраняем совместимость `/api/v1`.
- Добавляем endpoints уведомлений и расширенного поиска.

Примеры:
- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{id}/read`
- `GET /api/v1/catalog/tours?search=...` (поиск через Elasticsearch)

## Межсервисные контракты

- gRPC сохраняется для sync-запросов.
- Событийные контракты через RabbitMQ:
  - `BookingCreated`
  - `BookingStatusChanged`
  - `ProposalSent`
  - `ChatMessageSent`
