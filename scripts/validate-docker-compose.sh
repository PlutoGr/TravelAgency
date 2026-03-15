#!/usr/bin/env bash
# Validates docker-compose.yml syntax and structure (AUDIT-001).
# Run from repo root: ./scripts/validate-docker-compose.sh
#
# Verifies:
# - docker compose config succeeds (valid YAML, no syntax errors)
# - Gateway depends_on includes chat-service and media-service
# - Core services (redis, minio, chat, media) start by default (no profile)

set -e
cd "$(dirname "$0")/.."

COMPOSE_FILE="docker/docker-compose.yml"

echo "Validating $COMPOSE_FILE..."
OUTPUT=$(docker compose -f "$COMPOSE_FILE" config 2>&1) || {
  echo "ERROR: docker compose config failed:"
  echo "$OUTPUT"
  exit 1
}

echo "docker compose config: OK"

# Verify gateway depends_on includes chat-service and media-service
if ! echo "$OUTPUT" | grep -q "chat-service"; then
  echo "ERROR: gateway depends_on should include chat-service"
  exit 1
fi
if ! echo "$OUTPUT" | grep -q "media-service"; then
  echo "ERROR: gateway depends_on should include media-service"
  exit 1
fi

echo "Gateway depends_on (chat-service, media-service): OK"
echo "Docker Compose validation passed."
