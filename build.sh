#!/usr/bin/env bash

git checkout -- .
git pull origin

export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish -c Release --output bin

cd ./Volyar/bin && dotnet Volyar.dll --bootstrap && cd
