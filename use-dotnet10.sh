#!/usr/bin/env bash
# Use .NET 10 SDK for this repo (official install at /usr/local/share/dotnet).
# Homebrew dotnet@8 is often first in PATH and uses SDK 8.0.119; this script puts .NET 10 first.
#
# In this terminal only:
#   source use-dotnet10.sh   (or:  . use-dotnet10.sh)
#
# To make it permanent, add to ~/.zshrc (or ~/.bash_profile):
#   export PATH="/usr/local/share/dotnet:$PATH"
#
export PATH="/usr/local/share/dotnet:$PATH"
echo "Using: $(which dotnet) -> $(dotnet --version)"
