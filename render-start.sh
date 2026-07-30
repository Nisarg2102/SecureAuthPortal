#!/usr/bin/env bash

# Export paths so the runtime can find dotnet
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools

# Run the published application
echo "Starting the application..."
dotnet out/SecureAuthPortal.dll
