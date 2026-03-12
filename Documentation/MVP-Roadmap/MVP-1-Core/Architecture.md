# MVP-1 Core Architecture

## Цель этапа

Запустить рабочий продукт с ключевой ценностью:
- пользователь может найти тур;
- создать заявку;
- менеджер может взять заявку в работу и довести до подтверждения.

## Состав сервисов

- `api-gateway`
- `identity-service`
- `catalog-service`
- `booking-service`
- `chat-service` (опционально, но желательно)
- `media-service` (опционально, если нужны загрузки в V1)

## Межсервисное взаимодействие

- Синхронное: `gRPC`.
- Асинхронное: `Outbox + Background publisher` (без обязательного брокера).

## Инфраструктура

- Docker Compose
- PostgreSQL (разные БД по сервисам)
- Redis (по необходимости для chat backplane/кэша)
- MinIO (если включен media-service)

## Что НЕ входит в MVP-1

- Kafka/RabbitMQ как обязательная зависимость
- ClickHouse
- Kubernetes
- Сложная аналитика и BI-отчётность
