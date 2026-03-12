# MVP-1 API Contracts

## Внешний API (front -> gateway)

- REST/JSON, префикс `/api/v1`.
- Основные маршруты:
  - `/auth/*`
  - `/catalog/*`
  - `/bookings/*`
  - `/favorites/*`
  - `/chat/*` (если включен чат)

## Межсервисные контракты

- Синхронно: gRPC.
- Обязательные вызовы:
  - `Booking -> Catalog: GetTourSnapshot`
  - `Booking -> Identity: GetUserSummary`

## Правила версионирования

- Ломающие изменения: только в `/api/v2`.
- Неломающие: добавление необязательных полей.
