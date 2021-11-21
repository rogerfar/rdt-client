# Stage 1 - Build the frontend
FROM node:16-buster AS node-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appclient
WORKDIR /appclient

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
      dotnet restore -r linux-arm RdtClient.sln && dotnet publish -r linux-arm -c Release -o out ; \
   elif [ "$TARGETPLATFORM" = "linux/arm/v8"] ; then \
      echo "**** Building $TARGETPLATFORM arm v8 version" && \
      dotnet restore -r linux-arm64 RdtClient.sln && dotnet publish -r linux-arm64 -c Release -o out ; \
   else \
      echo "**** Building $TARGETPLATFORM x64 version" && \
      dotnet restore RdtClient.sln && dotnet publish -c Release -o out ; \
   fi

# Stage 3 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-ubuntu:focal
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
    apt-get update -y -qq && \
    echo "**** Install pre-reqs ****" && \
    apt-get install -y -qq wget dos2unix && \
    apt-get install -y libc6 libgcc1 libgssapi-krb5-2 libssl1.1 libstdc++6 zlib1g libicu66 && \
    echo "**** Installing dotnet ****" && \
    wget -q https://dot.net/v1/dotnet-install.sh && \
    chmod +x ./dotnet-install.sh && \
    bash ./dotnet-install.sh -c Current --runtime dotnet --install-dir /usr/share/dotnet && \
    bash ./dotnet-install.sh -c Current --runtime aspnetcore --install-dir /usr/share/dotnet && \
    echo "**** Cleaning image ****" && \
    apt-get -y -qq -o Dpkg::Use-Pty=0 clean && apt-get -y -qq -o Dpkg::Use-Pty=0 purge && \
    echo "**** Setting permissions ****" && \
    chown -R abc:abc /data && \
    rm -rf \
        /tmp/* \
        /var/lib/apt/lists/* \
        /var/tmp/* || true

ENV PATH "$PATH:/usr/share/dotnet"

WORKDIR /app
COPY --from=dotnet-build-env /appserver/server/out .
COPY --from=node-build-env /appclient/client/out ./wwwroot
COPY --from=node-build-env /appclient/root/ /

# ports and volumes
EXPOSE 6500
VOLUME ["/config", "/data" ]
