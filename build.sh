#!/bin/sh
set -eu

curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh

./dotnet-install.sh --install-dir "$PWD/dotnet"

export DOTNET_ROOT="$PWD/dotnet"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet --info
dotnet tool install --global h5-compiler
dotnet build app/LlmPrices.csproj -c Release

python3 scripts/build.py
cp current-v1.json wwwroot/current-v1.json
mkdir wwwroot/data
cp data/*.json wwwroot/data/
