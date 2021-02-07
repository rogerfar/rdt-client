# Stage 1 - Build the frontend
FROM amd64/node:15.5-buster AS node-build-env

RUN mkdir /appclient
WORKDIR /appclient

COPY client/. .
RUN npm ci
RUN npx ng build --prod --output-path=out

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:5.0.102-1-buster-slim-amd64 AS dotnet-build-env

RUN mkdir /appserver
WORKDIR /appserver

COPY server/. .

RUN if [ "$BUILDPLATFORM" = "arm/v7" ] ; then dotnet restore -r linux-arm RdtClient.sln ; else dotnet restore RdtClient.sln ; fi
RUN if [ "$BUILDPLATFORM" = "arm/v7" ] ; then dotnet publish -r linux-arm -c Release -o out ; else dotnet publish -c Release -o out ; fi

# Stage 3 - Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.2-buster-slim AS base
RUN mkdir /app
WORKDIR /app
COPY --from=dotnet-build-env /appserver/out .
COPY --from=node-build-env /appclient/out ./wwwroot
ENTRYPOINT ["dotnet", "RdtClient.Web.dll"]