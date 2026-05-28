FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble

ARG NODE_MAJOR=22
ARG ANGULAR_CLI_VERSION=21

RUN curl -fsSL "https://deb.nodesource.com/setup_${NODE_MAJOR}.x" | bash - \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs sqlite3 \
    && npm install -g "@angular/cli@^${ANGULAR_CLI_VERSION}" \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /data/db /data/downloads \
    && chown -R vscode:vscode /data
