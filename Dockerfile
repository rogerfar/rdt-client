# Stage 1 - Build the frontend
FROM amd64/node:15.5-buster AS node-build-env

RUN mkdir /appclient
WORKDIR /appclient

COPY client/. .
RUN npm ci
RUN npx ng build --prod --output-path=out

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:5.0.103-alpine3.13-amd64 AS dotnet-build-env

RUN mkdir /appserver
WORKDIR /appserver

COPY server/. .

RUN if [ "$BUILDPLATFORM" = "arm/v7" ] ; then dotnet restore -r linux-arm RdtClient.sln ; else dotnet restore RdtClient.sln ; fi
RUN if [ "$BUILDPLATFORM" = "arm/v7" ] ; then dotnet publish -r linux-arm -c Release -o out ; else dotnet publish -c Release -o out ; fi

# Stage 3 - Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.3-buster-slim AS base

RUN addgroup --quiet --gid 1000 dotnet
RUN adduser --system --uid 1000 --group dotnet --shell /bin/sh

RUN mkdir /app

WORKDIR /app
COPY --from=dotnet-build-env /appserver/out .
COPY --from=node-build-env /appclient/out ./wwwroot

RUN chown -R dotnet:dotnet /app

USER 1000

ENTRYPOINT ["dotnet", "RdtClient.Web.dll"]