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

Сейчас фронтенд не использует переменные окружения Vite. URL API задаётся через proxy в `vite.config.ts`:

```ts
server: {
  proxy: {
    '/api': { target: 'http://localhost:5000', changeOrigin: true }
  }
}
```

Для production можно добавить `VITE_API_URL` и использовать его в axios/fetch.

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
```

Unit/E2E тесты в проекте не настроены. Рекомендуется добавить Vitest/Jest и Playwright при необходимости.

## Scripts

| Команда | Описание |
|---------|----------|
| `npm run dev` | Запуск dev-сервера с HMR |
| `npm run build` | Сборка для production |
| `npm run preview` | Просмотр production-сборки |
| `npm run lint` | ESLint |

Полная инструкция по бэкенду — в [README репозитория](../../../README.md).
