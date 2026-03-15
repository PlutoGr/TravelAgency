# TravelAgency Gateway

API Gateway для TravelAgency. YARP reverse proxy, JWT-аутентификация, маршрутизация к микросервисам. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK в системе используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **Зависимые сервисы** — Identity (5010), Catalog (5020), Booking (5030), Chat (5040), Media (5050) для локального запуска

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Gateway)
./build.sh
```

`build.sh` собирает Gateway и запускает тесты. Сборка из корня: `dotnet build src/Gateway/TravelAgency.Gateway/TravelAgency.Gateway.csproj` (после `source scripts/use-dotnet10.sh`).

## Run

```bash
cd src/Gateway/TravelAgency.Gateway
dotnet run
```

Сервис доступен на `http://localhost:5000`. В режиме Development проксирует запросы на локальные порты сервисов (см. `appsettings.Development.json`).

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `JwtSettings__SigningKey` | — | **Обязательно.** Ключ подписи JWT (минимум 32 символа). Должен совпадать с Identity |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Окружение (Development/Production) |
| `Cookie__Secure` | `true` (prod) / `false` (dev) | Secure flag. Production: true (HTTPS). Development: false для localhost |
| `Cookie__SameSite` | `Lax` | SameSite policy (Lax, Strict, None) |
| `Cookie__Path` | `/` | Cookie path. `/` или `/api/v1` |
| `Cookie__Domain` | — | Опционально. Домен для cookie (напр. `.travelagency.com`) при разделении API и frontend по поддоменам |
| `RateLimiting__FailOpen` | `true` | При Redis rate limiting: `true` = разрешать запросы при недоступности Redis (fail-open); `false` = отклонять (fail-closed, строже по безопасности) |

Для локального запуска `JwtSettings__SigningKey` можно задать через user-secrets или переменные окружения.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY и др.
docker compose up gateway
```

Gateway: `http://localhost:5000`. Требуется `.env` с `JWT_SIGNING_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Gateway/TravelAgency.Gateway.Tests/
```

Или из этой папки: `./build.sh`.

## API Routes (через Gateway)

| Путь | Сервис | Авторизация |
|------|--------|-------------|
| `/api/v1/auth/*` | Identity | Anonymous |
| `/api/v1/catalog/*` (GET) | Catalog | Anonymous |
| `/api/v1/catalog/*` (POST/PUT/PATCH/DELETE) | Catalog | Manager/Admin |
| `/api/v1/bookings/*` | Booking | Authenticated |
| `/api/v1/favorites/*` | Booking | Authenticated |
| `/api/v1/chat/*` | Chat | Authenticated |
| `/api/v1/media/*` | Media | Authenticated |

Полная инструкция по сборке — в [README репозитория](../../README.md).
