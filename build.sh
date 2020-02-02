#!/usr/bin/env bash

git clean -f
git fetch origin
git reset --hard origin/master

chmod +x build.sh
chmod +x run.sh

export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish -c Release --output build

cd ./build && dotnet Volyar.dll --bootstrap && cd
