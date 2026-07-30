#!/usr/bin/env bash
# exit on error
set -o errexit

# Install .NET SDK 10.0
echo "Downloading and installing .NET 10.0 SDK..."
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 10.0

# Export paths for the current build session
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools

# Publish the application
echo "Publishing the application..."
dotnet publish SecureAuthPortal.csproj -c Release -o out
