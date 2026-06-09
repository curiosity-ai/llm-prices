#!/bin/sh
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -InstallDir ./dotnet
export PATH="$PATH:/opt/buildhome/.dotnet/tools:/usr/bin/dotnet:/opt/buildhome/repo/dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
./dotnet/dotnet --version
./dotnet/dotnet tool install --global h5-compiler
./dotnet/dotnet build app/LlmPrices.csproj -c Release
