# TravelAgency Catalog Service

Микросервис каталога туров и направлений. CRUD туров, цены, фильтрация. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **PostgreSQL** — база `travel_catalog` (создаётся через `docker/init-db.sql` или вручную)

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Services/Catalog)
./build.sh
```

`build.sh` собирает Catalog API и запускает unit/integration тесты. Сборка из корня: `dotnet build src/Services/Catalog/TravelAgency.Catalog.API/TravelAgency.Catalog.API.csproj` (после `source scripts/use-dotnet10.sh`).

## Run

```bash
cd src/Services/Catalog/TravelAgency.Catalog.API
dotnet run
```

Сервис доступен на `http://localhost:5020`.

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `ConnectionStrings__CatalogDb` | appsettings | Строка подключения PostgreSQL |
| `GrpcSettings__InternalServiceToken` | — | Токен для gRPC service-to-service auth (обязателен для gRPC) |
| `JwtSettings__SigningKey` | appsettings | Ключ подписи JWT (должен совпадать с Identity) |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `ASPNETCORE_RUN_MIGRATIONS` | `false` | `true` — автоматический запуск миграций при старте |
| `AllowedHosts` | `localhost` (prod) / `*` (dev) | В production задайте явный список хостов через env |

**Важно:** В production переопределите `JwtSettings__SigningKey` и `AllowedHosts` через переменные окружения.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY, POSTGRES_*
docker compose up catalog-service
```

Catalog: `http://localhost:5020`. Требуется `.env` с `JWT_SIGNING_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Services/Catalog/TravelAgency.Catalog.UnitTests/
dotnet test src/Services/Catalog/TravelAgency.Catalog.IntegrationTests/
```

Или из этой папки: `./build.sh`.

Полная инструкция по сборке — в [README репозитория](../../../README.md).
