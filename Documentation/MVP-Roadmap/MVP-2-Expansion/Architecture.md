# MVP-2 Expansion Architecture

## Цель этапа

Расширить продукт после первых пользователей: улучшить отклик, уведомления и управляемость.

## Добавляется к MVP-1

- `notification-service` как отдельный сервис.
- RabbitMQ + MassTransit для событийной интеграции.
- Elasticsearch для сложного поиска каталога.

## Обновлённые взаимодействия

- Синхронные критичные запросы: gRPC.
- Асинхронные доменные события: RabbitMQ.

## Инфраструктура

- Всё из MVP-1 +
  - `rabbitmq`
  - `notification-service`
  - `elasticsearch`
