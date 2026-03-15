# TravelAgency Media Service

Микросервис загрузки и управления медиафайлами. S3-совместимое хранилище (MinIO), автогенерация превью для изображений. Clean Architecture, DDD. Целевая платформа: **.NET 10**.

## Prerequisites

- **.NET 10 SDK** — при нескольких SDK используйте `source scripts/use-dotnet10.sh` из корня репозитория
- **PostgreSQL** — база `travel_media` (создаётся через `docker/init-db.sql` или вручную)
- **MinIO** (или AWS S3) — объектное хранилище
- **Identity service** — для JWT (генерация токенов)

## Building this project

```bash
# Из корня репозитория (опционально; build.sh делает это автоматически)
source scripts/use-dotnet10.sh

# Из этой папки (src/Services/Media)
./build.sh
```

`build.sh` собирает Media API и запускает unit/integration тесты. Полная инструкция — в [README репозитория](../../../README.md).

## Run

```bash
cd src/Services/Media/TravelAgency.Media.API
dotnet run
```

Сервис доступен на `http://localhost:5050` (или `http://localhost:5000` в зависимости от конфигурации). Swagger: `http://localhost:5050/swagger`.

## Environment Variables

| Переменная | По умолчанию | Описание |
|------------|--------------|----------|
| `ConnectionStrings__MediaDb` | **required** | Строка подключения PostgreSQL. Должна быть задана через переменную окружения. Пример: `Host=...;Port=5432;Database=travel_media;Username=...;Password=...` |
| `Storage__ServiceUrl` | `http://minio:9000` | URL MinIO/S3 |
| `Storage__AccessKey` | appsettings | Access key |
| `Storage__SecretKey` | appsettings | Secret key |
| `Storage__BucketName` | `travel-agency-media` | Имя bucket |
| `Storage__PresignTtlMinutes` | `60` | TTL presigned URL (минуты) |
| `JwtSettings__SigningKey` | appsettings | Ключ подписи JWT |
| `JwtSettings__Issuer` | `TravelAgency.Identity` | Issuer токена |
| `JwtSettings__Audience` | `TravelAgency` | Audience токена |
| `ASPNETCORE_RUN_MIGRATIONS` | `false` | `true` — автоматический запуск миграций при старте |

**База данных:** Media требует PostgreSQL (как и другие сервисы). Строка подключения **обязательно** задаётся через переменную окружения `ConnectionStrings__MediaDb` — в appsettings.json нет значения по умолчанию.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env   # заполните JWT_SIGNING_KEY, POSTGRES_*, MINIO_ROOT_*
docker compose --profile full up media-service
```

Media: `http://localhost:5050`. Требуется `.env` с `JWT_SIGNING_KEY`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`. PostgreSQL и MinIO должны быть запущены.

### Локальный MinIO

```bash
docker run -d -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"
```

Bucket создаётся автоматически при первом запуске Media.

## Tests

```bash
# Из корня репозитория
source scripts/use-dotnet10.sh
dotnet test src/Services/Media/TravelAgency.Media.UnitTests/
dotnet test src/Services/Media/TravelAgency.Media.IntegrationTests/
```

Или из этой папки: `./build.sh`.

**Ожидаемый результат:** ~62 unit + ~25 integration тестов.

## API Endpoints

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/media/upload` | Загрузка файла (multipart/form-data, JWT) |
| GET | `/media/{id}` | Скачивание файла |
| POST | `/media/presign` | Генерация presigned URL (JWT) |
| DELETE | `/media/{id}` | Удаление (только владелец, JWT) |
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe (проверка S3) |

## Project Structure

```
TravelAgency.Media.Domain/       → Entities, enums, domain exceptions
TravelAgency.Media.Application/ → Use cases, MediatR, validators, DTOs
TravelAgency.Media.Infrastructure/ → S3StorageService, ImageProcessingService
TravelAgency.Media.API/          → Controllers, middleware, DI
TravelAgency.Media.UnitTests/
TravelAgency.Media.IntegrationTests/
```

## Key Features

- **Upload:** multipart, валидация MIME и размера, автогенерация превью для изображений
- **Presigned URLs:** безопасный доступ без проксирования
- **Auth:** JWT, роли (Client, Manager, Admin), удаление только владельцем
- **Observability:** Serilog, OpenTelemetry, Correlation ID, health checks
