# TravelAgency

[![Deploy to dev](https://github.com/PlutoGr/TravelAgency/actions/workflows/deploy.yml/badge.svg?branch=main)](https://github.com/PlutoGr/TravelAgency/actions/workflows/deploy.yml)

Microservices solution (Gateway, Identity, Catalog, Booking, Chat, Media) and frontend. All backend projects target **.NET 10**.

Documentation: [GitHub wiki](https://github.com/PlutoGr/TravelAgency/wiki). Dev server, deploy and HTTPS: [Где и как работает](https://github.com/PlutoGr/TravelAgency/wiki/Doc-Infrastructure).

## Building the repository

The solution uses **.NET 10** (`global.json`). If the machine has multiple SDKs, the build may pick the wrong one. Use the repo helper so the correct SDK is used:

```bash
# From repository root
source scripts/use-dotnet10.sh   # or: . scripts/use-dotnet10.sh
./build.sh
```

`build.sh` builds the whole solution and runs all tests. You can also run `dotnet build TravelAgency.sln` and `dotnet test TravelAgency.sln` after sourcing `scripts/use-dotnet10.sh`.

### Building a single project

Each backend project has its own `build.sh` and README:

| Project   | Path                     | Build from folder |
|-----------|--------------------------|-------------------|
| Gateway   | `src/Gateway/`           | `./build.sh`      |
| Identity  | `src/Services/Identity/` | `./build.sh`      |
| Catalog   | `src/Services/Catalog/`  | `./build.sh`      |
| Booking   | `src/Services/Booking/`  | `./build.sh`      |
| Chat      | `src/Services/Chat/`     | `./build.sh`      |
| Media     | `src/Services/Media/`    | `./build.sh`      |

From the project folder (for example `src/Services/Identity`), run `./build.sh`. The script uses `scripts/use-dotnet10.sh` from the repo root automatically.

### Prerequisites

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download). If several SDKs are installed, run `scripts/use-dotnet10.sh` before building.
- Service dependencies (PostgreSQL, Redis, Docker) are listed in each service README. Local ports, migrations and seed behavior: [Локальный запуск](https://github.com/PlutoGr/TravelAgency/wiki/Doc-Local-Development).

### Required environment variables (local development)

When running services locally without Docker, set these before starting:

- `ConnectionStrings__IdentityDb`, `ConnectionStrings__CatalogDb`, `ConnectionStrings__BookingDb`, `ConnectionStrings__MediaDb`, `ConnectionStrings__DefaultConnection`: PostgreSQL connection strings (replace `Password=REPLACE_VIA_ENV` with a local password).
- `JwtSettings__SigningKey`: JWT signing key (min 32 chars). Required for Identity, Booking, Chat, Catalog, Media. Generate with `openssl rand -hex 32`.

Docker names and placeholders: [`docker/.env.example`](docker/.env.example) and [Локальный запуск](https://github.com/PlutoGr/TravelAgency/wiki/Doc-Local-Development).

### Running with Docker

From the `docker/` directory, copy `.env.example` to `.env`, set the required values, then run:

```bash
cd docker
cp .env.example .env
# Edit .env: JWT_SIGNING_KEY, GRPC_INTERNAL_SERVICE_TOKEN, passwords
docker compose up
```

This starts Gateway, Identity, Catalog, Booking, Chat, Media, PostgreSQL, Redis, and MinIO. Add `--profile full` to also start the frontend.

### Authentication

Tokens are stored in **httpOnly cookies**. Flow, CORS and cookie settings: [Вход и сессия](https://github.com/PlutoGr/TravelAgency/wiki/Doc-How-Auth).

### Test accounts (Development seed)

In Development, or when `ASPNETCORE_SEED_DATA=true`, Identity seeds these logins. The password is `Seeding__TestUserPassword` and is not stored in the repository. What the seed creates and how it updates existing rows: [Локальный запуск](https://github.com/PlutoGr/TravelAgency/wiki/Doc-Local-Development).

| Email             | Role    |
|-------------------|---------|
| client@test.com   | Client  |
| manager@test.com  | Manager |
| manager2@test.com | Manager |
| admin@test.com    | Admin   |

## Solution structure

- `src/Gateway/`: API Gateway
- `src/Services/`: Identity, Catalog, Booking, Chat, Media
- `src/Shared/`: Shared contracts and gRPC
- `src/Frontend/travel-agency-frontend/`: React + Vite frontend (Node.js)
- `docker/`: Docker Compose for local run
- `scripts/use-dotnet10.sh`: Ensures .NET 10 is used in the current shell
