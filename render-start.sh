#!/usr/bin/env bash

# Export paths so the runtime can find dotnet from the local directory
export DOTNET_ROOT=$PWD/dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools

# Run the published application
echo "Starting the application..."
dotnet out/SecureAuthPortal.dll
