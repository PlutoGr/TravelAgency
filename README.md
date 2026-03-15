# TravelAgency

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

### Authentication (cookie-based)

Tokens are stored in **httpOnly cookies** (XSS-safe). See [Cookie-Based Auth](ai_docs/develop/features/cookie-auth.md) for flow, CORS, and production config.

## Solution structure

- `src/Gateway/` — API Gateway
- `src/Services/` — Identity, Catalog, Booking, Chat, Media
- `src/Shared/` — Shared contracts and gRPC
- `src/Frontend/travel-agency-frontend/` — React + Vite frontend (Node.js)
- `docker/` — Docker Compose for local run
- `scripts/use-dotnet10.sh` — Ensures .NET 10 is used in the current shell
