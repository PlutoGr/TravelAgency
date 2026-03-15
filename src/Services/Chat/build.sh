#!/usr/bin/env bash
# Build and test the Chat service using .NET 10.
# Run from this directory or from repo root. See README.md for full instructions.
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
cd "$REPO_ROOT"
if [[ -f "$REPO_ROOT/scripts/use-dotnet10.sh" ]]; then source "$REPO_ROOT/scripts/use-dotnet10.sh"; fi
dotnet build "$SCRIPT_DIR/TravelAgency.Chat.API/TravelAgency.Chat.API.csproj" "$@"
dotnet test "$SCRIPT_DIR/TravelAgency.Chat.UnitTests/TravelAgency.Chat.UnitTests.csproj" --no-build --verbosity normal
dotnet test "$SCRIPT_DIR/TravelAgency.Chat.IntegrationTests/TravelAgency.Chat.IntegrationTests.csproj" --no-build --verbosity normal
