# Stage 1 - Install Frontend Dependencies
FROM node:18-alpine3.18 AS node-deps
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir -p /appclient/client
WORKDIR /appclient/client

COPY client/package.json client/package-lock.json ./
RUN \
   echo "**** Installing Frontend Dependencies  ****" && \
   npm ci

# Stage 2 - Build Frontend
FROM node-deps AS node-build-env

WORKDIR /appclient/client

COPY client .
RUN \
   echo "**** Building Frontend ****" && \
   npm run build -- --output-path=out

# Stage 3 - Install Backend Dependencies
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS dotnet-deps
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir -p /appserver/server
WORKDIR /appserver/server

COPY server/RdtClient.sln ./
COPY server/RdtClient.Data/RdtClient.Data.csproj RdtClient.Data/RdtClient.Data.csproj
COPY server/RdtClient.Service/RdtClient.Service.csproj RdtClient.Service/RdtClient.Service.csproj
COPY server/RdtClient.Web/RdtClient.Web.csproj RdtClient.Web/RdtClient.Web.csproj

RUN \
   echo "**** Installing Backend Dependencies for $TARGETPLATFORM on $BUILDPLATFORM ****" && \
   dotnet restore --no-cache RdtClient.Web/RdtClient.Web.csproj

# Stage 4 - Build the backend
FROM dotnet-deps AS dotnet-build-env

WORKDIR /appserver/server

COPY server .
RUN \
   echo "**** Building Backend Code for $TARGETPLATFORM on $BUILDPLATFORM ****" && \
   dotnet publish RdtClient.Web/RdtClient.Web.csproj --no-restore -c Release -o out ;

# Stage 5 - Install dotnet runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS dotnet-runtime

# Stage 6 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-alpine:3.20
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

# set version label
ARG BUILD_DATE
ARG VERSION
LABEL build_version="Linuxserver.io extended version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="ravensorb"

# set environment variables
ARG DEBIAN_FRONTEND="noninteractive"
ENV XDG_CONFIG_HOME="/config/xdg"
ENV RDTCLIENT_BRANCH="main"

RUN \
   mkdir -p /data/downloads /data/db || true && \
   echo "**** Updating package information ****" && \
   apk update && \
   echo "**** Install pre-reqs ****" && \
   apk add bash icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib

COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
ENV PATH="$PATH:/usr/share/dotnet"

RUN \
   echo "**** Setting permissions ****" && \
   chown -R abc:abc /data && \
   rm -rf \
   /tmp/* \
   /var/cache/apk/* \
   /var/tmp/* || true

# Copy files for app
WORKDIR /app
COPY --from=dotnet-build-env /appserver/server/out .
COPY --from=node-build-env /appclient/client/out/browser ./wwwroot
COPY ./root/ /

# ports and volumes
EXPOSE 6500

# Check Status
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 CMD curl --fail http://localhost:6500 || exit 
