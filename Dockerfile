# Stage 1 - Build runtime image
FROM ghcr.io/linuxserver/baseimage-mono:LTS

# set version label
ARG BUILD_DATE
ARG VERSION
ARG RDTCLIENT_VERSION
LABEL build_version="Linuxserver.io version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="ravensorb"

# set environment variables
ARG DEBIAN_FRONTEND="noninteractive"
ENV XDG_CONFIG_HOME="/config/xdg"

RUN mkdir /app || true && ln -s /config /data && mkdir -p /data/downloads /data/db || true && \
    echo "**** Updating package information ****" && \ 
      apt update -y -qq && \
    echo "**** install packages ****" && \
      apt install -y -qq wget jq && \
    echo "**** Installing dotnet ****" && \
      wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb  && \
      dpkg -i packages-microsoft-prod.deb 2> /dev/null && \
      rm packages-microsoft-prod.deb && \
      apt update -y -qq && \
      apt install -y -qq apt-transport-https dotnet-runtime-5.0 aspnetcore-runtime-5.0 && \
    echo "**** install rtd-client ****" && \
      if [ -z ${RDTCLIENT_VERSION+x} ]; then \
         RDTCLIENT_VERSION=$(curl -sX GET https://api.github.com/repos/rogerfar/rdt-client/releases/latest | jq -r ".name"); \
      fi && \
      curl -o \
         /tmp/RealDebridClient.zip -L \
         "https://github.com/rogerfar/rdt-client/releases/download/${RDTCLIENT_VERSION}/RealDebridClient.zip" && \
      unzip /tmp/RealDebridClient.zip -d /app && \
    echo "**** cleanup ****" && \
      apt-get -y -qq -o Dpkg::Use-Pty=0 clean && apt-get -y -qq -o Dpkg::Use-Pty=0 purge && \
      rm -rf \
         /tmp/* \
         /var/lib/apt/lists/* \
         /var/tmp/* || true

WORKDIR /data

# add local files
COPY root/ /

# ports and volumes
EXPOSE 6500
VOLUME ["/config", "/data" ]
