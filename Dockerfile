# Stage 1 - Build the frontend
FROM node:12.15-buster AS node-build-env

RUN mkdir /appclient
WORKDIR /appclient

COPY client/. .
RUN npm ci
RUN npx ng build --prod --output-path=out

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS dotnet-build-env

RUN mkdir /appserver
WORKDIR /appserver

COPY server/. .
RUN dotnet restore RdtClient.sln
RUN dotnet build -c Release
RUN dotnet publish -c Release -o out

# Stage 3 - Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.1-buster-slim AS base
RUN mkdir /app
WORKDIR /app
COPY --from=dotnet-build-env /appserver/out .
COPY --from=node-build-env /appclient/out ./wwwroot
ENTRYPOINT ["dotnet", "RdtClient.Web.dll"]