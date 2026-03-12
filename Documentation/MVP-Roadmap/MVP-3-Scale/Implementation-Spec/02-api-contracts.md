# MVP-3 API Contracts

## Новые внешние API

- `GET /api/v1/analytics/kpi`
- `GET /api/v1/analytics/funnel`
- `GET /api/v1/analytics/export`

## Внутренние контракты

- gRPC для оперативных cross-service запросов.
- События для аналитического ingestion:
  - регистрация пользователя;
  - просмотр тура;
  - создание/изменение заявки;
  - отправка предложения.

## Совместимость

- Без ломки существующих `/api/v1` сценариев.
- Новые поля и endpoints добавляются backward-compatible.
