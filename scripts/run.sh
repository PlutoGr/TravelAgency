#!/usr/bin/env bash
# Starts the full TravelAgency solution via Docker (all backend services + frontend).
# Run from repo root: ./scripts/run.sh
#
# Prerequisites:
#   - Docker and Docker Compose
#   - docker/.env with JWT_SIGNING_KEY, GRPC_INTERNAL_SERVICE_TOKEN, POSTGRES_PASSWORD
#
# If docker/.env does not exist, copy from docker/.env.example and fill in the values.

set -e
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/docker"

if [[ ! -f .env ]]; then
  echo "ERROR: docker/.env not found."
  echo "Copy from docker/.env.example:"
  echo "  cp docker/.env.example docker/.env"
  echo "Then edit docker/.env and set JWT_SIGNING_KEY, GRPC_INTERNAL_SERVICE_TOKEN, POSTGRES_PASSWORD"
  exit 1
fi

# Validate docker-compose
if [[ -f "$ROOT/scripts/validate-docker-compose.sh" ]]; then
  "$ROOT/scripts/validate-docker-compose.sh" || true
fi

# Optional: --backend-only to skip frontend (avoids npm network issues in Docker)
BACKEND_ONLY=false
for arg in "$@"; do
  [[ "$arg" == "--backend-only" ]] && BACKEND_ONLY=true && break
done

if [[ "$BACKEND_ONLY" == "true" ]]; then
  echo "Starting TravelAgency (backend only, no frontend)..."
  docker compose up -d --build
  echo ""
  echo "  Gateway:   http://localhost:5001"
  echo "  Frontend:  run separately: cd src/Frontend/travel-agency-frontend && npm run dev"
else
  echo "Starting TravelAgency (backend + frontend)..."
  docker compose --profile full up -d --build
  echo ""
  echo "  Frontend:  http://localhost:3000"
  echo "  Gateway:   http://localhost:5001"
  echo "  MinIO:     http://localhost:9001"
fi

echo ""
echo "Services starting. Wait ~60 seconds for all services to be ready."
echo ""
echo "To view logs: docker compose logs -f"
echo "To stop:      docker compose down"
echo ""
echo "If build fails with 'cannot allocate memory', build services one by one:"
echo "  for s in identity-service catalog-service booking-service chat-service media-service frontend; do docker compose build \$s; done"
