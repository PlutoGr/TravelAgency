# TravelAgency Identity Service

ASP.NET Core microservice responsible for authentication and user management.

## Local Development Setup

### Prerequisites

- .NET 10 SDK
- PostgreSQL 17 (or run via Docker: `docker compose -f docker/docker-compose.yml up postgres`)

### Configure Secrets (required before running)

Sensitive values are **not** stored in source control. The `appsettings.Development.json`
file contains placeholder values that must be overridden before the service will start.

**Using .NET User Secrets (recommended for local dev):**

```bash
cd src/Services/Identity/TravelAgency.Identity.API

dotnet user-secrets set "ConnectionStrings:IdentityDb" "Host=localhost;Port=5432;Database=travel_identity;Username=travel_admin;Password=<your-password>"
dotnet user-secrets set "JwtSettings:SigningKey" "<your-signing-key-min-32-chars>"
dotnet user-secrets set "GrpcSettings:InternalServiceToken" "<your-grpc-token>"
```

**Using environment variables:**

```
ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=travel_identity;Username=travel_admin;Password=<your-password>
JwtSettings__SigningKey=<your-signing-key-min-32-chars>
GrpcSettings__InternalServiceToken=<your-grpc-token>
```

### Run the service

```bash
cd src/Services/Identity/TravelAgency.Identity.API
dotnet run
```

The service starts on `http://localhost:5010` by default.

## Project Structure

```
TravelAgency.Identity.API/          → Presentation layer (controllers, middleware, DI)
TravelAgency.Identity.Application/  → Use cases, commands/queries, validators
TravelAgency.Identity.Domain/       → Entities, domain events, value objects
TravelAgency.Identity.Infrastructure/ → EF Core, JWT, external service adapters
```
