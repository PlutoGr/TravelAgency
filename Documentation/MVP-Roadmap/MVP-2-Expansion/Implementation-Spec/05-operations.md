# MVP-2 Operations

## Инфраструктурные добавления

- Запуск RabbitMQ и Elasticsearch в окружениях этапа.
- Алерты на:
  - рост DLQ;
  - рост latency поиска;
  - отставание consumers.

## Release strategy

- Пошаговый rollout notification-service.
- Feature flags для новых каналов уведомлений.
- Прогрев индекса перед переключением на Elasticsearch.
