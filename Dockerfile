# Stage 1 - Build the frontend
FROM node:lts-alpine AS node-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appclient
WORKDIR /appclient

RUN apk add --no-cache git python3 py3-pip make g++

COPY client ./client
COPY root ./root
RUN \
   cd client && \
   echo "**** Building Code  ****" && \
   npm install && \
   npx ng build --output-path=out

RUN ls -FCla /appclient/root

# Stage 2 - Build the backend
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS dotnet-build-env
ARG TARGETPLATFORM
ENV TARGETPLATFORM=${TARGETPLATFORM:-linux/amd64}
ARG BUILDPLATFORM
ENV BUILDPLATFORM=${BUILDPLATFORM:-linux/amd64}

RUN mkdir /appserver
WORKDIR /appserver

COPY server ./server
RUN \
   echo "**** Building Source Code for $TARGETPLATFORM on $BUILDPLATFORM ****" && \
   cd server && \
   dotnet restore --no-cache RdtClient.sln && \
   dotnet test && \
   dotnet publish --no-restore -c Release -o out ; 

# Stage 3 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-alpine:3.20@sha256:37c85bdd640fd71bf95221f8a8c8cffb151cd91ae114e9917a690a4e702344c3
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
   apk add bash icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib && \
   echo "**** Installing dotnet ****" && \
   mkdir -p /usr/share/dotnet

# Checksums are published by Microsoft alongside each tarball at <tarball-url>.sha512.
# When bumping ASPNETCORE_VERSION, refresh all three with:
#   curl -s https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/<version>/aspnetcore-runtime-<version>-linux-musl-<rid>.tar.gz.sha512
ARG ASPNETCORE_VERSION=10.0.0
ARG ASPNETCORE_SHA512_X64=4f18b7fd126212364b7ca68ca3f3f0d63358cc87172fd72fe77de256788d9340814421c16303172248f0c7297b9b96bc0694175c41ef78ce221b7bd02cf94ca4
ARG ASPNETCORE_SHA512_ARM64=a0a00bd51bbb13d40edde7076df7f04e586d92ead979d89e888177d8d816e6756ac80d9491d74e8a4ced71e425c7530e9ed8d944ec0dfb18bf25ccc21d2ac502
ARG ASPNETCORE_SHA512_ARM=7c16240e845544fc793877e3998716ba39000846d450937065fc3f467df9a6ff5482cebfe28757b71e1e519abf92459ea4816f5549577fa9a4e258ca3ca7e401

RUN \
   set -eu && \
   if [ "$TARGETPLATFORM" = "linux/arm/v7" ] ; then \
   RID="arm" ; SHA512="$ASPNETCORE_SHA512_ARM" ; \
   elif [ "$TARGETPLATFORM" = "linux/arm64" ] ; then \
   RID="arm64" ; SHA512="$ASPNETCORE_SHA512_ARM64" ; \
   else \
   RID="x64" ; SHA512="$ASPNETCORE_SHA512_X64" ; \
   fi && \
   TARBALL="aspnetcore-runtime-${ASPNETCORE_VERSION}-linux-musl-${RID}.tar.gz" && \
   echo "**** Downloading $TARBALL ****" && \
   wget -q "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/${ASPNETCORE_VERSION}/${TARBALL}" && \
   echo "**** Verifying checksum ****" && \
   echo "${SHA512}  ${TARBALL}" | sha512sum -c - && \
   tar zxf "$TARBALL" -C /usr/share/dotnet && \
   rm "$TARBALL"

RUN \
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
COPY --from=node-build-env /appclient/client/out/browser ./wwwroot
COPY --from=node-build-env /appclient/root/ /

# ports and volumes
EXPOSE 6500

# Check Status
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 CMD curl --fail http://localhost:6500 || exit 1
