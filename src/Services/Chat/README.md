# TravelAgency Chat Service

Микросервис чата в реальном времени для бронирований. REST API для истории сообщений, SignalR для live-сообщений. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **PostgreSQL** — база `travel_chat` для хранения сообщений
- **Redis** — для health checks (опционально; пустая строка отключает)
- **Booking service** — для проверки доступа к бронированию (HTTP)

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Services/Chat)
./build.sh
```

`build.sh` собирает Chat API и запускает unit/integration тесты. Полная инструкция — в [README репозитория](../../../README.md).

## Run

```bash
cd src/Services/Chat/TravelAgency.Chat.API
dotnet run
```

Сервис доступен на `http://localhost:5040`.

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `ConnectionStrings__ChatDb` | appsettings | Строка подключения PostgreSQL |
| `ConnectionStrings__DefaultConnection` | appsettings | Альтернативная строка подключения |
| `ConnectionStrings__Redis` | `redis:6379` | Redis для health check |
| `Services__BookingServiceUrl` | `http://localhost:5030` | URL Booking API |
| `JwtSettings__SigningKey` | appsettings | Ключ подписи JWT |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `ASPNETCORE_RUN_MIGRATIONS` | `false` | `true` — автоматический запуск миграций при старте |

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY, POSTGRES_*
docker compose --profile full up chat-service
```

Chat: `http://localhost:5040`. Требуется `.env` с `JWT_SIGNING_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Services/Chat/TravelAgency.Chat.UnitTests/
dotnet test src/Services/Chat/TravelAgency.Chat.IntegrationTests/
```

Или из этой папки: `./build.sh`.

**Примечание:** Integration тесты используют TestContainers (требуется Docker). Некоторые тесты могут быть нестабильны — известная проблема для follow-up.

## API

### REST (через Gateway)

- **GET** `/api/v1/chat/booking/{bookingId}/messages` — список сообщений (JWT обязателен)

### SignalR (прямое подключение)

- **URL:** `http://localhost:5040/hubs/chat` (локально) или `http://chat-service:8080/hubs/chat` (Docker)
- **Методы:** `JoinBookingGroup(bookingId)`, `SendMessage(bookingId, text, attachments?)`
- **Событие:** `MessageReceived` — получение новых сообщений

### Health Checks

- `/health/live` — liveness
- `/health/ready` — readiness (DB + опционально Redis)

## Sample Requests

```bash
# Получить сообщения (требуется валидный JWT)
curl -H "Authorization: Bearer <token>" \
  http://localhost:5040/chat/booking/{bookingId}/messages
```

SignalR: используйте `@microsoft/signalr`; передавайте `access_token` в query string для WebSocket auth.
