# TravelAgency.Media Microservice

A file upload/download/management microservice backed by S3-compatible storage (MinIO). Implements Clean Architecture with DDD principles for managing media files including automatic thumbnail generation for images.

## Service Overview

**TravelAgency.Media** provides REST endpoints for file operations with:
- Multipart file uploads with validation
- Automatic image thumbnail generation (300×300, 100×100)
- Presigned URL generation for secure sharing
- File streaming and download
- Soft-delete support
- JWT-based authentication and role-based authorization
- Health checks (liveness & readiness)
- OpenTelemetry tracing
- Structured logging with Serilog

## Architecture

The service follows **Clean Architecture** with clear separation of concerns:

```
TravelAgency.Media/
├── Domain/                   DDD aggregate: MediaFile entity, enums, domain exceptions
├── Application/              Use cases, behaviors, abstractions, DTOs
├── Infrastructure/           External service implementations (S3, ImageSharp)
├── API/                      REST controllers, middleware, configuration
├── UnitTests/                Domain, application, and validator tests
└── IntegrationTests/         API endpoint and middleware integration tests
```

### Layers

- **Domain** (`TravelAgency.Media.Domain`)
  - `MediaFile` aggregate with factory methods and business logic
  - `MediaFileStatus` enum (Active, Deleted)
  - Domain exceptions

- **Application** (`TravelAgency.Media.Application`)
  - MediatR commands/queries for Upload, Get, Presign, Delete operations
  - FluentValidation validators
  - Behavior pipeline: Logging → Validation → Handler
  - Abstractions: `IStorageService`, `IImageProcessingService`, `ICurrentUserService`
  - Settings: `StorageSettings` for S3 configuration
  - DTOs: `UploadMediaResponse`, `PresignedUrlDto`, etc.

- **Infrastructure** (`TravelAgency.Media.Infrastructure`)
  - `S3StorageService`: AWS SDK implementation for MinIO/S3 operations
  - `ImageProcessingService`: SixLabors.ImageSharp for thumbnail generation
  - `CurrentUserService`: JWT claim extraction
  - Dependency injection configuration

- **API** (`TravelAgency.Media.API`)
  - `MediaController`: 4 RESTful endpoints
  - Auth extensions: JWT bearer token validation
  - Middleware: Correlation ID, Global exception handler
  - Health checks (liveness at `/health/live`, readiness at `/health/ready`)

## API Endpoints

### Upload File
```
POST /media/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

Response: 201 Created
{
  "id": "userId/uuid/filename",
  "fileName": "photo.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245632,
  "thumbnailIds": ["userId/uuid/filename-thumb-300x300", "userId/uuid/filename-thumb-100x100"]
}
```

### Download File
```
GET /media/{id}

Response: 200 OK
Content-Type: image/jpeg (or appropriate media type)
[file content]
```

### Generate Presigned URL
```
POST /media/presign
Authorization: Bearer <token>
Content-Type: application/json

{
  "mediaId": "userId/uuid/filename",
  "ttlSeconds": 3600
}

Response: 200 OK
{
  "url": "http://minio:9000/travel-agency-media/...",
  "expiresAt": "2026-03-14T21:14:30Z"
}
```

### Delete File
```
DELETE /media/{id}
Authorization: Bearer <token>
Authorization Policy: Manager or Admin role required

Response: 204 No Content
```

### Health Check - Liveness
```
GET /health/live

Response: 200 OK
Probes: No checks (always ready)
```

### Health Check - Readiness
```
GET /health/ready

Response: 200 OK or 503 Service Unavailable
Probes: S3 bucket connectivity
```

## Configuration

### appsettings.json

```json
{
  "Storage": {
    "BucketName": "travel-agency-media",
    "ServiceUrl": "http://minio:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "ForcePathStyle": true,
    "MaxFileSizeBytes": 104857600,
    "AllowedMimeTypes": [
      "image/jpeg",
      "image/png",
      "image/webp",
      "image/gif",
      "application/pdf",
      "application/msword"
    ],
    "ThumbnailSizes": [
      { "Width": 300, "Height": 300 },
      { "Width": 100, "Height": 100 }
    ],
    "PresignedUrlTtlSeconds": 3600
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "TravelAgency.Identity",
    "Audience": "TravelAgency.Media",
    "AccessTokenExpirationMinutes": 60
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"]
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

For production deployments, override via environment variables:

```bash
# Storage (S3/MinIO)
Storage__ServiceUrl=https://s3.amazonaws.com
Storage__AccessKey=AKIAIOSFODNN7EXAMPLE
Storage__SecretKey=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
Storage__BucketName=travel-agency-prod-media
Storage__MaxFileSizeBytes=104857600

# JWT
Jwt__SecretKey=prod-secret-key
Jwt__Issuer=TravelAgency.Identity
Jwt__Audience=TravelAgency.Media

# Logging
Serilog__MinimumLevel=Information
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- MinIO server (or AWS S3 credentials)
- Identity service running (for JWT token generation)

### With dotnet CLI

```bash
# Navigate to API project
cd src/Services/Media/TravelAgency.Media.API

# Restore dependencies
dotnet restore

# Run the service
dotnet run

# Service starts at http://localhost:5000
# Swagger UI available at http://localhost:5000/swagger
```

### With Docker

```bash
# Build image
docker build -t travelagency-media:latest -f src/Services/Media/TravelAgency.Media.API/Dockerfile .

# Run container
docker run -p 5000:8080 \
  -e Storage__ServiceUrl=http://minio:9000 \
  -e Storage__AccessKey=minioadmin \
  -e Storage__SecretKey=minioadmin \
  -e Jwt__SecretKey=your-secret \
  travelagency-media:latest
```

### Local Development with MinIO

```bash
# Start MinIO in Docker
docker run -d \
  -p 9000:9000 \
  -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"

# Create bucket
docker exec <minio-container> \
  mc mb minio/travel-agency-media

# Service config in appsettings.Development.json:
{
  "Storage": {
    "ServiceUrl": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "travel-agency-media"
  }
}
```

## Running Tests

### Unit Tests

Test domain entities, application handlers, validators, and behaviors in isolation (no external dependencies).

```bash
cd src/Services/Media/TravelAgency.Media.UnitTests

# Run all unit tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=MediaFileEntityTests"

# Expected: 62 tests passing
```

Test coverage:
- `MediaFileEntityTests`: Entity creation, state transitions, business logic
- `UploadMediaCommandHandlerTests`: File validation, MIME types, size limits, thumbnail generation
- `GetMediaQueryHandlerTests`: Stream retrieval, not-found scenarios
- `PresignMediaCommandHandlerTests`: URL generation, TTL validation
- `DeleteMediaCommandHandlerTests`: Soft-delete operations
- `UploadMediaCommandValidatorTests`: Input validation rules
- `PresignMediaCommandValidatorTests`: TTL range validation
- `LoggingBehaviorTests`: Pipeline behavior logging
- `ImageProcessingServiceTests`: Thumbnail generation logic

### Integration Tests

Test API endpoints with mocked storage (no real S3 calls).

```bash
cd src/Services/Media/TravelAgency.Media.IntegrationTests

# Run all integration tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=MediaUploadIntegrationTests"

# Expected: 25 tests passing
```

Test coverage:
- `MediaUploadIntegrationTests`: Upload endpoint with auth, validation, file size limits
- `MediaPresignIntegrationTests`: Presign endpoint, TTL verification
- `CorrelationIdMiddlewareTests`: Request correlation ID tracking
- `GlobalExceptionHandlerMiddlewareTests`: Error response formatting (ProblemDetails)

### Run All Tests

```bash
dotnet test src/Services/Media/

# Expected results:
# - UnitTests: 62 passed
# - IntegrationTests: 25 passed
# - Total: 87 tests passing
```

## Project Structure

```
src/Services/Media/
├── TravelAgency.Media.Domain/
│   ├── Entities/
│   │   ├── MediaFile.cs                 (DDD aggregate)
│   │   └── MediaFileThumbnail.cs
│   ├── Enums/
│   │   └── MediaFileStatus.cs           (Active, Deleted)
│   ├── Exceptions/
│   │   ├── MediaNotFoundException.cs
│   │   ├── MediaAccessDeniedException.cs
│   │   └── MediaDomainException.cs
│   └── Interfaces/
│       └── IMediaFileRepository.cs
│
├── TravelAgency.Media.Application/
│   ├── Abstractions/
│   │   ├── IStorageService.cs           (Upload, Download, Delete, Presign)
│   │   ├── IImageProcessingService.cs   (Thumbnail generation)
│   │   └── ICurrentUserService.cs       (JWT claims)
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs        (FluentValidation pipeline)
│   │   └── LoggingBehavior.cs           (Request/response logging)
│   ├── Settings/
│   │   └── StorageSettings.cs           (Config binding)
│   ├── Features/
│   │   ├── Upload/
│   │   │   ├── UploadMediaCommand.cs
│   │   │   ├── UploadMediaCommandHandler.cs
│   │   │   └── UploadMediaCommandValidator.cs
│   │   ├── Get/
│   │   │   ├── GetMediaQuery.cs
│   │   │   └── GetMediaQueryHandler.cs
│   │   ├── Presign/
│   │   │   ├── PresignMediaCommand.cs
│   │   │   ├── PresignMediaCommandHandler.cs
│   │   │   └── PresignMediaCommandValidator.cs
│   │   └── Delete/
│   │       ├── DeleteMediaCommand.cs
│   │       └── DeleteMediaCommandHandler.cs
│   ├── DTOs/
│   │   ├── UploadMediaResponse.cs
│   │   ├── PresignedUrlDto.cs
│   │   └── MediaFileDto.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   ├── AppValidationException.cs
│   │   └── ValidationException.cs
│   └── DependencyInjection.cs           (MediatR, validators registration)
│
├── TravelAgency.Media.Infrastructure/
│   ├── Storage/
│   │   └── S3StorageService.cs          (AWS SDK for MinIO/S3)
│   ├── Services/
│   │   ├── ImageProcessingService.cs    (SixLabors.ImageSharp)
│   │   └── CurrentUserService.cs        (HttpContext.User claims)
│   ├── Repositories/
│   │   └── InMemoryMediaFileRepository.cs
│   └── DependencyInjection.cs           (Infrastructure service registration)
│
├── TravelAgency.Media.API/
│   ├── Controllers/
│   │   └── MediaController.cs           (4 endpoints)
│   ├── Extensions/
│   │   ├── AuthenticationExtensions.cs
│   │   ├── AuthorizationExtensions.cs
│   │   ├── HealthCheckExtensions.cs
│   │   ├── SerilogExtensions.cs
│   │   ├── SwaggerExtensions.cs
│   │   ├── CorsExtensions.cs
│   │   └── TracingExtensions.cs
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── GlobalExceptionHandlerMiddleware.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Dockerfile
│
├── TravelAgency.Media.UnitTests/
│   ├── Domain/
│   │   └── MediaFileEntityTests.cs
│   ├── Application/
│   │   ├── Features/
│   │   │   ├── UploadMediaCommandHandlerTests.cs
│   │   │   ├── GetMediaQueryHandlerTests.cs
│   │   │   ├── PresignMediaCommandHandlerTests.cs
│   │   │   └── DeleteMediaCommandHandlerTests.cs
│   │   ├── Validators/
│   │   │   ├── UploadMediaCommandValidatorTests.cs
│   │   │   └── PresignMediaCommandValidatorTests.cs
│   │   └── Behaviors/
│   │       └── LoggingBehaviorTests.cs
│   └── Infrastructure/
│       └── Services/
│           └── ImageProcessingServiceTests.cs
│
└── TravelAgency.Media.IntegrationTests/
    ├── CustomWebApplicationFactory.cs
    ├── MediaUploadIntegrationTests.cs
    ├── MediaPresignIntegrationTests.cs
    └── Middleware/
        ├── CorrelationIdMiddlewareTests.cs
        └── GlobalExceptionHandlerMiddlewareTests.cs
```

## Key Features

### Authentication & Authorization

- JWT bearer token validation (from TravelAgency.Identity service)
- Role-based access control (Client, Manager, Admin)
- Upload/Presign endpoints: Any authenticated user
- Delete endpoint: Manager or Admin role required
- Public download endpoint (presigned URLs enable secure sharing)

### File Management

- **Storage Key Format**: `{ownerId}/{uuid}/{sanitized-filename}`
  - Automatically scopes files to user
  - Prevents collisions
  - Enables IAM-based access patterns

- **Thumbnail Generation** (images only)
  - Automatic generation on upload
  - Configurable sizes: 300×300, 100×100
  - Stored alongside originals with `-thumb-{width}x{height}` suffix

- **Presigned URLs**
  - Generated by MinIO/S3 SDK
  - Configurable TTL (default 1 hour, max 7 days)
  - No service acts as reverse proxy

### Observability

- **Structured Logging**: Serilog with JSON output
- **OpenTelemetry Tracing**: ActivitySource for distributed tracing
- **Health Checks**: Liveness probe, Readiness probe (tests S3 connectivity)
- **Correlation IDs**: Request-scoped tracing via middleware

### Error Handling

- RFC 7807 ProblemDetails responses
- Validation errors: 400 with field details
- File not found: 404
- Unauthorized: 401
- Forbidden: 403
- Server errors: 500 with CorrelationId

## Dependencies

### NuGet Packages

Core:
- `MediatR.Contracts` (v14)
- `FluentValidation` (v12)
- `Microsoft.Extensions.Options` (v10)

Storage:
- `AWSSDK.S3` (v4) — MinIO/AWS S3 operations

Image Processing:
- `SixLabors.ImageSharp` (v3) — Thumbnail generation

Observability:
- `Serilog` (v10) — Structured logging
- `System.Diagnostics.DiagnosticSource` (v10) — OpenTelemetry tracing
- `OpenTelemetry.Exporter.Console` — Console trace export (dev)

Testing:
- `xunit` — Unit test framework
- `Moq` — Mocking library
- `FluentAssertions` — Assertion library
- `Microsoft.AspNetCore.Mvc.Testing` — WebApplicationFactory for integration tests

## Architecture Decisions

1. **No Database in v1**
   - `MediaFile` domain entity created in handler but not persisted
   - S3 storage key serves as media ID
   - Future: Add PostgreSQL for metadata queries

2. **Storage Key Format**
   - `{ownerId}/{uuid}/{sanitized-filename}` structure
   - User scoping + collision prevention + future IAM patterns

3. **Thumbnail Strategy**
   - Generated only for image MIME types
   - Multiple sizes per configuration
   - Returned as array in upload response

4. **Public Download Endpoint**
   - `/media/{id}` intentionally public
   - Enables presigned URL sharing
   - Access control via TTL and unguessable keys (UUIDs)

5. **IAmazonS3 Singleton**
   - Thread-safe AWS SDK client
   - Expensive to construct
   - Injected into scoped `S3StorageService`

## Development Notes

- **Target Framework**: .NET 10
- **Namespace Root**: `TravelAgency.Media.*`
- **Nullable Reference Types**: Enabled
- **Line Endings**: CRLF
- **Encoding**: UTF-8 with BOM

## Troubleshooting

### MinIO Connection Failures
- Verify `Storage:ServiceUrl` and credentials in appsettings
- Check MinIO container is running: `docker ps | grep minio`
- Test connectivity: `aws s3 --endpoint-url http://localhost:9000 ls`

### JWT Validation Failures
- Verify `Jwt:SecretKey` matches Identity service
- Check token expiration: `JWT.io` decoder
- Ensure `Jwt:Issuer` and `Jwt:Audience` match Identity claims

### File Upload Failures
- Check `Storage:MaxFileSizeBytes` limit
- Verify MIME type in `Storage:AllowedMimeTypes`
- Review `GlobalExceptionHandlerMiddleware` response for details

### Thumbnail Generation Issues
- Verify image format is JPEG, PNG, WebP, or GIF
- Check `Storage:ThumbnailSizes` configuration
- Review ImageSharp memory usage for large batches

## Support & Contributing

For issues or feature requests, refer to the main TravelAgency repository documentation.

Test results and implementation details are available in:
- Unit tests: `src/Services/Media/TravelAgency.Media.UnitTests/`
- Integration tests: `src/Services/Media/TravelAgency.Media.IntegrationTests/`

---

**Status**: ✅ Production Ready (v1.0)  
**Last Updated**: 2026-03-14  
**Architecture**: Clean Architecture with DDD  
**Test Coverage**: 87 tests (62 unit + 25 integration)
