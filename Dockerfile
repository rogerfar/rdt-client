# Stage 1 - Build the frontend
FROM node:18-alpine3.18 AS node-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appclient
WORKDIR /appclient

RUN apk add --no-cache git python3 py3-pip make g++

RUN \
   echo "**** Cloning Source Code ****" && \
   git clone https://github.com/rogerfar/rdt-client.git . && \
   cd client && \
   echo "**** Building Code  ****" && \
   npm ci && \
   npx ng build --output-path=out

RUN ls -FCla /appclient/root

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim-amd64 AS dotnet-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appserver
WORKDIR /appserver

RUN \
   echo "**** Cloning Source Code ****" && \
   git clone https://github.com/rogerfar/rdt-client.git . && \
   echo "**** Building Source Code for $TARGETPLATFORM on $BUILDPLATFORM ****" && \
   cd server && \
   if [ "$TARGETPLATFORM" = "linux/arm/v7" ] ; then \
      echo "**** Building $TARGETPLATFORM arm v7 version" && \
      dotnet restore --no-cache -r linux-arm RdtClient.sln && dotnet publish --no-restore -r linux-arm -c Release -o out ; \
   elif [ "$TARGETPLATFORM" = "linux/arm/v8" ] ; then \
      echo "**** Building $TARGETPLATFORM arm v8 version" && \
      dotnet restore --no-cache -r linux-arm64 RdtClient.sln && dotnet publish --no-restore -r linux-arm64 -c Release -o out ; \
   else \
      echo "**** Building $TARGETPLATFORM x64 version" && \
      dotnet restore --no-cache RdtClient.sln && dotnet publish --no-restore -c Release -o out ; \
   fi

# Stage 3 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-alpine:3.18
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
    apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib && \
    echo "**** Installing dotnet ****" && \
    apk add aspnetcore6-runtime && \
    echo "**** Setting permissions ****" && \
    chown -R abc:abc /data && \
    rm -rf \
        /tmp/* \
        /var/cache/apk/* \
        /var/tmp/* || true

ENV PATH "$PATH:/usr/share/dotnet"

# Copy files for app
WORKDIR /app
COPY --from=dotnet-build-env /appserver/server/out .
COPY --from=node-build-env /appclient/client/out ./wwwroot
COPY --from=node-build-env /appclient/root/ /

# ports and volumes
EXPOSE 6500
VOLUME [ "/data" ]

# Check Status
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 CMD curl --fail http://localhost:6500 || exit 