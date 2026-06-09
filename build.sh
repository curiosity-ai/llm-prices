#!/bin/sh
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -InstallDir ./dotnet
./dotnet/dotnet --version
./dotnet/dotnet publish -c Release -o output
./dotnet/dotnet tool install --global h5-compiler
./dotnet/dotnet build app/LlmPrices.csproj -c Release
