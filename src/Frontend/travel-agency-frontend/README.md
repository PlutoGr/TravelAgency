# TravelAgency Frontend

React + TypeScript + Vite фронтенд для TravelAgency. Tailwind CSS, React Query, React Router.

## Prerequisites

- **Node.js** (npm) — для сборки и разработки
- **Gateway** — API на `http://localhost:5000` (или настройте proxy в `vite.config.ts`)

Бэкенд (.NET 10): из корня репозитория `source scripts/use-dotnet10.sh` и `./build.sh`.

## Building this project

```bash
# Из этой папки (src/Frontend/travel-agency-frontend)
npm install
npm run build
```

## Run

```bash
# Режим разработки (HMR)
npm run dev
```

Приложение доступно на `http://localhost:5173`. API-запросы проксируются на `http://localhost:5000` (см. `vite.config.ts`).

## Environment Variables

| Variable             | Required | Description                                                                 |
|----------------------|----------|-----------------------------------------------------------------------------|
| *(none)*             | No       | **Dev**: No env vars needed. Vite proxy forwards `/api` to `localhost:5000` |
| `VITE_API_URL`       | No       | **Production**: API base URL when frontend and API run on different origins. Default: `/api/v1` (relative). Example: `http://localhost:5000/api/v1` for Docker when frontend is on port 3000 and Gateway on 5000. Pass as build arg: `docker build --build-arg VITE_API_URL=...` |
| `VITE_CHAT_HUB_URL`  | No       | **SignalR hub override**: If Gateway does not proxy WebSockets, set to direct Chat service hub URL (e.g. `http://localhost:5040/hubs/chat`). Default: `/api/v1/chat/hubs/chat` (relative). |

**Dev**: `vite.config.ts` proxies `/api` → `http://localhost:5000`. No env vars required.

**Production**: Set `VITE_API_URL` at build time when frontend and API are on different origins. The API client uses `import.meta.env.VITE_API_URL || '/api/v1'`.

## Docker

```bash
# Из корня репозитория
cd docker
cp .env.example .env
docker compose --profile full up frontend
```

Frontend: `http://localhost:3000`. Запускается вместе с Gateway и сервисами.

## Tests

```bash
# Линтинг
npm run lint

# Unit-тесты (Vitest)
npm run test        # watch mode
npm run test:run    # single run
```

**FE-001 config verification** (из корня репозитория):

```bash
./scripts/verify-fe001-config.sh
```

## Scripts

| Команда | Описание |
|---------|----------|
| `npm run dev` | Запуск dev-сервера с HMR |
| `npm run build` | Сборка для production |
| `npm run preview` | Просмотр production-сборки |
| `npm run lint` | ESLint |
| `npm run test` | Vitest (watch) |
| `npm run test:run` | Vitest (single run) |

Полная инструкция по бэкенду — в [README репозитория](../../../README.md).
