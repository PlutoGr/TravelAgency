#!/usr/bin/env bash
# Build Identity API using .NET 10 SDK (avoids PATH/dotnet@8 issues in Cursor terminal).
set -e
# Clear any env that could force SDK 8; then force .NET 10
unset MSBuildSDKsPath DOTNET_CLI_HOME
export DOTNET_ROOT="/usr/local/share/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
# Force MSBuild to use SDK 10 (avoids 8.0.119 being chosen when both are present)
for SDK_PATH in "$DOTNET_ROOT/sdk/10.0.201/Sdks" "/opt/homebrew/Cellar/dotnet@8/8.0.119/libexec/sdk/10.0.201/Sdks"; do
  if [[ -d "$SDK_PATH" ]]; then
    export MSBuildSDKsPath="$SDK_PATH"
    break
  fi
done
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"
exec "$DOTNET_ROOT/dotnet" build src/Services/Identity/TravelAgency.Identity.API/TravelAgency.Identity.API.csproj "$@"
