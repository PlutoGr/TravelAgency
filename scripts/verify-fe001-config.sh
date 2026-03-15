#!/usr/bin/env bash
# FE-001 config verification: CORS, proxy, nginx, env docs.
# Run from repo root: ./scripts/verify-fe001-config.sh

set -e
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

ERRORS=0

# 1. Vite proxy /api -> localhost:5000
VITE_CFG="src/Frontend/travel-agency-frontend/vite.config.ts"
if [[ ! -f "$VITE_CFG" ]]; then
  echo "FAIL: $VITE_CFG not found"
  ERRORS=$((ERRORS + 1))
elif ! grep -q "proxy" "$VITE_CFG" || ! grep -q "'/api'" "$VITE_CFG" || ! grep -q "localhost:5000" "$VITE_CFG"; then
  echo "FAIL: $VITE_CFG missing proxy /api -> localhost:5000"
  ERRORS=$((ERRORS + 1))
else
  echo "OK: Vite proxy /api -> localhost:5000"
fi

# 2. nginx /api/ -> gateway:8080
NGINX_CFG="src/Frontend/travel-agency-frontend/nginx.conf"
if [[ ! -f "$NGINX_CFG" ]]; then
  echo "FAIL: $NGINX_CFG not found"
  ERRORS=$((ERRORS + 1))
elif ! grep -q "location /api/" "$NGINX_CFG" || ! grep -q "gateway:8080" "$NGINX_CFG"; then
  echo "FAIL: $NGINX_CFG missing /api/ -> gateway:8080"
  ERRORS=$((ERRORS + 1))
else
  echo "OK: nginx /api/ -> gateway:8080"
fi

# 3. Gateway CORS: localhost:5173, localhost:3000
GATEWAY_CORS="src/Gateway/TravelAgency.Gateway/appsettings.Development.json"
if [[ ! -f "$GATEWAY_CORS" ]]; then
  echo "FAIL: $GATEWAY_CORS not found"
  ERRORS=$((ERRORS + 1))
elif ! grep -q "localhost:5173" "$GATEWAY_CORS" || ! grep -q "localhost:3000" "$GATEWAY_CORS"; then
  echo "FAIL: $GATEWAY_CORS missing CORS origins for 5173 and 3000"
  ERRORS=$((ERRORS + 1))
else
  echo "OK: Gateway CORS allows localhost:5173, localhost:3000"
fi

# 4. Frontend README env vars documentation
README="src/Frontend/travel-agency-frontend/README.md"
if [[ ! -f "$README" ]]; then
  echo "FAIL: $README not found"
  ERRORS=$((ERRORS + 1))
elif ! grep -qi "Environment Variables" "$README" || ! grep -q "VITE_API_URL" "$README"; then
  echo "FAIL: $README missing env vars documentation"
  ERRORS=$((ERRORS + 1))
else
  echo "OK: README env vars documented"
fi

if [[ $ERRORS -gt 0 ]]; then
  echo ""
  echo "FE-001 config verification failed ($ERRORS error(s))"
  exit 1
fi

echo ""
echo "FE-001 config verification passed"
