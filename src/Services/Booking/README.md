# TravelAgency Booking Service

Микросервис бронирований. Создание, подтверждение, изменение статусов бронирований и предложений. Интеграция с Catalog через gRPC. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **PostgreSQL** — база `travel_booking` (создаётся через `docker/init-db.sql` или вручную)
- **Catalog service** — для проверки туров (gRPC)

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Services/Booking)
./build.sh
```

`build.sh` собирает Booking API и запускает unit (включая Infrastructure), integration тесты. Сборка из корня: `dotnet build src/Services/Booking/TravelAgency.Booking.API/TravelAgency.Booking.API.csproj` (после `source scripts/use-dotnet10.sh`).

## Run

```bash
cd src/Services/Booking/TravelAgency.Booking.API
dotnet run
```

Сервис доступен на `http://localhost:5030`.

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `ConnectionStrings__BookingDb` | appsettings | Строка подключения PostgreSQL |
| `JwtSettings__SigningKey` | — | **Обязательно в production.** Ключ подписи JWT (должен совпадать с Identity) |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `GrpcClients__CatalogServiceUrl` | `http://catalog-service:8080` | URL Catalog gRPC сервиса |
| `ASPNETCORE_RUN_MIGRATIONS` | `false` | `true` — автоматический запуск миграций при старте |

Для локального запуска Catalog: `GrpcClients__CatalogServiceUrl=http://localhost:5020`.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY, POSTGRES_*
docker compose up booking-service
```

Booking: `http://localhost:5030`. Требуется `.env` с `JWT_SIGNING_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`. Catalog должен быть запущен.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Services/Booking/TravelAgency.Booking.UnitTests/
dotnet test src/Services/Booking/TravelAgency.Booking.Infrastructure.UnitTests/
dotnet test src/Services/Booking/TravelAgency.Booking.IntegrationTests/
```

Или из этой папки: `./build.sh`.

Полная инструкция по сборке — в [README репозитория](../../../README.md).
