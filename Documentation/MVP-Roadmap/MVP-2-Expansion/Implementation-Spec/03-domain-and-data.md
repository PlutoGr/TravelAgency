# MVP-2 Domain and Data

## Новые сущности

- `Notification`:
  - `NotificationId`
  - `UserId`
  - `Type`
  - `Payload`
  - `IsRead`
  - `CreatedAt`

## Изменения в моделях

- Booking события становятся официальным интеграционным контрактом.
- Каталог поддерживает search index projection.

## Консистентность

- Eventual consistency между booking/chat/notification.
- Повторная доставка сообщений должна быть безопасной (idempotent consumer).
