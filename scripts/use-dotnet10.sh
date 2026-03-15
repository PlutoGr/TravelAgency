#!/usr/bin/env bash
# Ensures the current shell uses .NET 10 SDK for this repository.
# Use when the machine has multiple SDKs (e.g. .NET 8 in PATH) and the build picks the wrong one.
#
# Usage (from repo root or any subdirectory):
#   source scripts/use-dotnet10.sh
#   # or: . scripts/use-dotnet10.sh
#
# Then run: dotnet build, dotnet test, or ./build.sh

set -e
unset MSBuildSDKsPath
export DOTNET_MULTILEVEL_LOOKUP=0

# If current dotnet is already 10.x, use its root
CURRENT_DOTNET=""
if command -v dotnet &>/dev/null; then
  VER="$(dotnet --version 2>/dev/null || true)"
  if [[ "$VER" == 10.* ]]; then
    # Resolve path to dotnet binary and get DOTNET_ROOT (parent of dotnet)
    CURRENT_DOTNET="$(cd "$(dirname "$(command -v dotnet)")/.." && pwd)"
  fi
fi

if [[ -n "$CURRENT_DOTNET" && -d "$CURRENT_DOTNET/sdk" ]]; then
  export DOTNET_ROOT="$CURRENT_DOTNET"
  export PATH="$DOTNET_ROOT:$PATH"
  echo "Using .NET 10: $DOTNET_ROOT -> $(dotnet --version)"
  exit 0
fi

# Search common installation roots for SDK 10 (use find to avoid zsh glob "no matches" errors)
for ROOT in "$HOME/.dotnet" "/usr/local/share/dotnet" "/opt/homebrew/share/dotnet"; do
  if [[ ! -d "$ROOT/sdk" ]]; then continue; fi
  while IFS= read -r -d '' SDK_DIR; do
    if [[ -d "$SDK_DIR/Sdks" ]]; then
      export DOTNET_ROOT="$ROOT"
      export PATH="$DOTNET_ROOT:$PATH"
      export MSBuildSDKsPath="$SDK_DIR/Sdks"
      echo "Using .NET 10: $DOTNET_ROOT (SDK $(basename "$SDK_DIR")) -> $(dotnet --version)"
      exit 0
    fi
  done < <(find "$ROOT/sdk" -maxdepth 1 -type d -name '10.*' -print0 2>/dev/null)
done

echo "No .NET 10 SDK found. Install from https://dotnet.microsoft.com/download or ensure dotnet in PATH is 10.x."
exit 1
