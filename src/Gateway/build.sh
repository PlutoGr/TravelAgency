#!/usr/bin/env bash
# Build and test the Gateway project using .NET 10.
# Run from this directory or from repo root. See README.md for full instructions.
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"
if [[ -f "$REPO_ROOT/scripts/use-dotnet10.sh" ]]; then source "$REPO_ROOT/scripts/use-dotnet10.sh"; fi
dotnet build "$SCRIPT_DIR/TravelAgency.Gateway/TravelAgency.Gateway.csproj" "$@"
dotnet test "$SCRIPT_DIR/TravelAgency.Gateway.Tests/TravelAgency.Gateway.Tests.csproj" --no-build --verbosity normal
