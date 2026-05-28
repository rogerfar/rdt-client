#!/usr/bin/env bash

set -euo pipefail

sudo install -d -m 0775 -o vscode -g vscode \
  /data/db \
  /data/downloads \
  /home/vscode/.nuget \
  /home/vscode/.nuget/NuGet \
  /home/vscode/.nuget/packages \
  /home/vscode/.npm

sudo chown -R vscode:vscode /data /home/vscode/.nuget /home/vscode/.npm

dotnet restore server/RdtClient.sln
npm install --prefix client
