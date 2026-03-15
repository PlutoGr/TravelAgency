#!/usr/bin/env bash
# Build and test the entire solution using .NET 10.
# Run from repository root. Uses scripts/use-dotnet10.sh if available.
#
# Usage: ./build.sh

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [[ -f "$SCRIPT_DIR/scripts/use-dotnet10.sh" ]]; then
  source "$SCRIPT_DIR/scripts/use-dotnet10.sh"
fi

echo "Building solution..."
dotnet build TravelAgency.sln --no-incremental

echo "Running tests..."
dotnet test TravelAgency.sln --no-build --verbosity normal

echo "Done. Build and tests completed successfully."
