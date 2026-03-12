# MVP-1 Security and Reliability

## Security baseline

- JWT access + refresh.
- RBAC роли: `Client`, `Manager`, `Admin`.
- HTTPS-only в production.
- CORS whitelist.
- Маскирование PII в логах.

## Межсервисная безопасность

- gRPC service-to-service auth: mTLS и/или internal JWT.
- Внутренние endpoint не доступны извне.

## Надёжность

- Timeout/retry/circuit breaker на gRPC клиентах.
- Idempotency key для критичных `POST`.
- Outbox в booking-service минимум.
