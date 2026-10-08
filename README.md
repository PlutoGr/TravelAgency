# TravelAgency

[![Deploy to dev](https://github.com/PlutoGr/TravelAgency/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/PlutoGr/TravelAgency/actions/workflows/deploy.yml)

Microservices solution (Gateway, Identity, Catalog, Booking, Chat, Media) and frontend. All backend projects target **.NET 10**.

## Building the repository

The solution uses **.NET 10** (`global.json`). If your machine has multiple SDKs (e.g. .NET 8 in `PATH`), the build may pick the wrong one. Use the repo helper so the correct SDK is used:

```bash
# From repository root
source scripts/use-dotnet10.sh   # or: . scripts/use-dotnet10.sh
./build.sh
```

`build.sh` builds the whole solution and runs all tests. You can also run `dotnet build TravelAgency.sln` and `dotnet test TravelAgency.sln` after sourcing `scripts/use-dotnet10.sh`.

### Building a single project

Each backend project has its own `build.sh` and README:

| Project   | Path                     | Build from folder        |
|----------|---------------------------|---------------------------|
| Gateway  | `src/Gateway/`            | `./build.sh`              |
| Identity | `src/Services/Identity/`  | `./build.sh`              |
| Catalog  | `src/Services/Catalog/`   | `./build.sh`              |
| Booking  | `src/Services/Booking/`   | `./build.sh`              |
| Chat     | `src/Services/Chat/`      | `./build.sh`              |
| Media    | `src/Services/Media/`     | `./build.sh`              |

From the project folder (e.g. `src/Services/Identity`), run `./build.sh`. The script uses `scripts/use-dotnet10.sh` from the repo root automatically.

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download). If you have multiple SDKs, use `scripts/use-dotnet10.sh` before building.
- See each service README for dependencies (PostgreSQL, Redis, Docker, etc.).

### Required environment variables (local development)

When running services locally (without Docker), set these before starting:

- `ConnectionStrings__IdentityDb`, `ConnectionStrings__CatalogDb`, `ConnectionStrings__BookingDb`, `ConnectionStrings__MediaDb`, `ConnectionStrings__DefaultConnection` — PostgreSQL connection strings (replace `Password=REPLACE_VIA_ENV` with your password).
- `JwtSettings__SigningKey` — JWT signing key (min 32 chars). Required for Identity, Booking, Chat, Catalog, Media. Generate with: `openssl rand -hex 32`.

See `docker/.env.example` for Docker-based setup.

### Running with Docker

From the `docker/` directory, copy `.env.example` to `.env`, set the required values, then run:

```bash
cd docker
cp .env.example .env
# Edit .env with real JWT_SIGNING_KEY, GRPC_INTERNAL_SERVICE_TOKEN, passwords
docker compose up
```

This starts Gateway, Identity, Catalog, Booking, Chat, Media, PostgreSQL, Redis, and MinIO. Add `--profile full` to also start the frontend.

### Authentication (cookie-based)

Tokens are stored in **httpOnly cookies** (XSS-safe). See [Cookie-Based Auth](ai_docs/develop/features/cookie-auth.md) for flow, CORS, and production config.

### Test accounts (Development seed)

When running in Development or with `ASPNETCORE_SEED_DATA=true`, Identity and Catalog seed test data. Logins:

| Email             | Role    |
|-------------------|---------|
| client@test.com   | Client  |
| manager@test.com  | Manager |
| manager2@test.com | Manager |
| admin@test.com    | Admin   |

The password is not stored in the repository. Set `Seeding__TestUserPassword` for Identity (environment variable or user secrets). Without it new seed users are skipped; accounts that already exist keep their hash.

Catalog seeds directions (Мальдивы, Пхукет, Санторини, Бали, Дубай), five published demo tours owned by `manager@test.com`, and one draft owned by `manager2@test.com`.
Catalog demo data alone (without test users) can be enabled with `Seeding__DemoCatalog=true`. Media uses the same flag to store demo photos.

## Как устроен dev

Dev-среда работает на одном сервере Selectel (Ubuntu 24.04) в Docker Compose. Код и образы обновляются автоматически после каждого push в `main`.

### Адрес и доступ

Адрес и доступ у команды. Сертификат самоподписанный (домена пока нет, см. #17), поэтому браузер покажет предупреждение, его нужно принять один раз. Сайт закрыт basic auth; без пароля открыты только `/robots.txt` и `/healthz`.

- HTTP на 80 порту редиректит на HTTPS. Поисковики сайт не индексируют (`robots.txt` и заголовок `X-Robots-Tag`).
- Снаружи открыты только порты 22, 80 и 443 (ufw). Gateway (`127.0.0.1:5001`) и консоль MinIO (`127.0.0.1:9001`) доступны только через SSH-туннель:

```bash
ssh -L 5001:127.0.0.1:5001 -L 9001:127.0.0.1:9001 travelagency-dev-platon
```

### Окружение

- Все .NET-сервисы на dev работают с `ASPNETCORE_ENVIRONMENT=Staging` (задаётся в `docker-compose.override.yml` на сервере). Swagger и OpenAPI выключены, в ответах 500 нет стека, подробные логи Development не используются.
- Миграции применяются при старте (`ASPNETCORE_RUN_MIGRATIONS=true`).
- Тестовые аккаунты и демо-каталог на dev оставлены специально: включены флагом `ASPNETCORE_SEED_DATA=true` (таблица аккаунтов выше). Пароль задаётся `Seeding__TestUserPassword` и в репозиторий не входит. На production флаг не ставить.
- Cookie авторизации с флагом `Secure` (сайт только по HTTPS), CORS разрешён для адреса dev-сайта (адрес у команды).

### SSH

Вход только по ключу, по паролю и под root нельзя, fail2ban банит перебор. Алиасы из `~/.ssh/config` на Mac Платона:

| Алиас | Пользователь | Для чего |
|---|---|---|
| `travelagency-dev-platon` | `platon` | Администрирование, есть `sudo` и группа `docker` |
| `travelagency-dev-deploy` | `deploy` | Пользователь деплоя (ключ в секрете `DEPLOY_SSH_KEY`), группа `docker`, без `sudo` |

Адрес (HostName) у команды.

```
Host travelagency-dev-platon
    User platon
    IdentityFile ~/.ssh/travelagency-dev
    IdentitiesOnly yes
```

### Как работает деплой

Workflow [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) запускается на каждый push в `main`:

1. `dotnet test` всего решения. Если тесты падают, дальше не идёт.
2. Сборка 7 образов (gateway, identity, catalog, booking, chat, media, frontend) и пуш в GHCR с тегами `latest` и полным sha коммита: `ghcr.io/plutogr/travelagency-<имя>:<sha>`.
3. По SSH под `deploy`: `git pull` в `/opt/travelagency`, `docker compose pull` и `docker compose up -d` с `IMAGE_TAG=<sha>`, удаление старых образов.
4. Проверка здоровья: HTTP отдаёт 301, HTTPS без пароля 401, `/healthz` отвечает `Healthy` (gateway и все сервисы), API отвечает 200. Если за 5 минут не дождались, пайплайн красный.

Отдельно на каждый push работает [поиск секретов gitleaks](.github/workflows/gitleaks.yml), а Dependabot раз в неделю присылает PR с обновлениями ([`.github/dependabot.yml`](.github/dependabot.yml)).

Ручной запуск: Actions → Deploy to dev → Run workflow.

### Откат на прошлую версию

Образы каждого коммита хранятся в GHCR с тегом sha, поэтому откат не требует пересборки.

- **Через GitHub Actions (основной способ):** Actions → Deploy to dev → Run workflow, в поле `rollback_sha` указать полный sha коммита (40 символов), на который откатываемся. Тесты и сборка пропускаются, на сервер ставятся образы этого коммита и выполняется та же проверка здоровья.
- **Вручную на сервере** (образы в GHCR приватные, нужен свой токен GitHub с правом `read:packages`):

```bash
ssh travelagency-dev-deploy
docker login ghcr.io -u <github-логин>    # пароль: токен с read:packages
cd /opt/travelagency/docker
export IMAGE_TAG=<полный sha>
docker compose pull && docker compose up -d
docker logout ghcr.io
```

Следующий обычный деплой вернёт свежую версию. Если откат нужен надолго, лучше сделать `git revert` плохого коммита и запушить его в `main`.

### Где смотреть логи

```bash
ssh travelagency-dev-platon
cd /opt/travelagency/docker
docker compose ps                         # статус контейнеров
docker compose logs -f --tail=200 gateway # логи сервиса (gateway, identity-service, frontend, ...)
```

Логи контейнеров ротируются Docker (`/etc/docker/daemon.json`: `max-size 10m`, `max-file 3`), диск они не забьют. Логи сборки и деплоя находятся во вкладке Actions.

### Файлы, которые есть только на сервере

Их нет в репозитории (секреты или настройки конкретного сервера). Бэкапы настраиваются в #18.

| Файл на сервере | Что это | Откуда восстановить |
|---|---|---|
| `/opt/travelagency/docker/.env` | Секреты: пароли Postgres и MinIO, `JWT_SIGNING_KEY`, `GRPC_INTERNAL_SERVICE_TOKEN` | Шаблон `docker/.env.example`, значения сгенерировать заново (`openssl rand -base64 32`) |
| `/opt/travelagency/docker/docker-compose.override.yml` | Порты, образы GHCR, монтирование сертификата и пароля | [`docker/docker-compose.override.example.yml`](docker/docker-compose.override.example.yml) |
| `/etc/travelagency/certs/tls.crt`, `tls.key` | Самоподписанный сертификат для адреса dev (адрес у команды), до 2028-10-02 | `openssl req -x509 -newkey rsa:2048 -nodes -days 730 -subj "/CN=<адрес>" -addext "subjectAltName=IP:<адрес>" -keyout tls.key -out tls.crt` |
| `/etc/travelagency/auth/htpasswd` | Пароль basic auth для `dev` | `printf 'dev:%s\n' "$(openssl passwd -apr1)" > /etc/travelagency/auth/htpasswd` |
| `/opt/travelagency-local/minio/Dockerfile` | Сборка локального образа MinIO | [`docker/minio/Dockerfile`](docker/minio/Dockerfile) |
| `/etc/docker/daemon.json` | Ротация логов Docker | `{"log-driver": "json-file", "log-opts": {"max-size": "10m", "max-file": "3"}}` |
| `/etc/ssh/sshd_config.d/00-hardening.conf`, `/etc/fail2ban/jail.local` | Настройки SSH и fail2ban | Раздел «SSH» выше |

Системные обновления безопасности ставит `unattended-upgrades` каждый день. Если после обновления ядра нужна перезагрузка, появится файл `/var/run/reboot-required`.

## Solution structure

- `src/Gateway/` — API Gateway
- `src/Services/` — Identity, Catalog, Booking, Chat, Media
- `src/Shared/` — Shared contracts and gRPC
- `src/Frontend/travel-agency-frontend/` — React + Vite frontend (Node.js)
- `docker/` — Docker Compose for local run
- `scripts/use-dotnet10.sh` — Ensures .NET 10 is used in the current shell
