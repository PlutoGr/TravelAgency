# TravelAgency Identity Service

Микросервис аутентификации и управления пользователями. JWT, refresh tokens, регистрация, логин, профиль. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **PostgreSQL 17** — или через Docker: `docker compose -f docker/docker-compose.yml up postgres`

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Services/Identity)
./build.sh
```

`build.sh` собирает Identity API и запускает unit/integration тесты. Полная инструкция — в [README репозитория](../../../README.md).

## Run

```bash
cd src/Services/Identity/TravelAgency.Identity.API
dotnet run
```

Сервис доступен на `http://localhost:5010`.

### Настройка секретов (обязательно перед запуском)

Чувствительные данные не хранятся в репозитории. Переопределите значения из `appsettings.Development.json`.

**User Secrets (рекомендуется для локальной разработки):**

```bash
cd src/Services/Identity/TravelAgency.Identity.API

dotnet user-secrets set "ConnectionStrings:IdentityDb" "Host=localhost;Port=5432;Database=travel_identity;Username=travel_admin;Password=<your-password>"
dotnet user-secrets set "JwtSettings:SigningKey" "<your-signing-key-min-32-chars>"
dotnet user-secrets set "GrpcSettings:InternalServiceToken" "<your-grpc-token>"
```

**Переменные окружения:**

```
ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=travel_identity;Username=travel_admin;Password=<your-password>
JwtSettings__SigningKey=<your-signing-key-min-32-chars>
GrpcSettings__InternalServiceToken=<your-grpc-token>
```

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `ConnectionStrings__IdentityDb` | appsettings | Строка подключения PostgreSQL |
| `JwtSettings__SigningKey` | — | **Обязательно.** Ключ подписи JWT (минимум 32 символа) |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `GrpcSettings__InternalServiceToken` | — | Токен для gRPC service-to-service auth |
| `ASPNETCORE_RUN_MIGRATIONS` | `false` | `true` — автоматический запуск миграций при старте |

**Примечание:** Docker Compose переопределяет `ConnectionStrings__IdentityDb` на `Host=postgres;Database=travel_identity;...`.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY, GRPC_INTERNAL_SERVICE_TOKEN, POSTGRES_*
docker compose up identity-service
```

Identity: `http://localhost:5010`. Требуется `.env` с `JWT_SIGNING_KEY`, `GRPC_INTERNAL_SERVICE_TOKEN`, `POSTGRES_USER`, `POSTGRES_PASSWORD`.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Services/Identity/TravelAgency.Identity.UnitTests/
dotnet test src/Services/Identity/TravelAgency.Identity.IntegrationTests/
```

Или из этой папки: `./build.sh`.

## Project Structure

```
TravelAgency.Identity.API/          → Presentation (controllers, middleware, DI)
TravelAgency.Identity.Application/  → Use cases, commands/queries, validators
TravelAgency.Identity.Domain/       → Entities, domain events, value objects
TravelAgency.Identity.Infrastructure/ → EF Core, JWT, external adapters
```
