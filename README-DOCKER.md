# [rogerfar/rdt-client](https://github.com/rogerfar/rdt-client)
Thid docker file follows the [linuxserver.io](https://linuxserver.io) pattern that leverages the [s6-overlay](https://github.com/just-containers/s6-overlay) to run the application as a service within the container.  This allows for scripts to be run prior to start of the application to handle initalization and setting of permissions. 

rdt-client is a web a web interface to manage your torrents on Real-Debrid. It supports the following features:

* Add new torrents through magnets or files
* Download all files from Real Debrid to your local machine automatically
* Unpack all files when finished downloading
* Implements a fake qBittorrent API so you can hook up other applications like Sonarr or Couchpotato.
* Built with Angular 11 and .NET 5

## Supported Architectures

Our images support multiple architectures such as `x86-64`, `arm64` and `armhf`. We utilise the docker manifest for multi-platform awareness. More information is available from docker [here](https://github.com/docker/distribution/blob/master/docs/spec/manifest-v2-2.md#manifest-list) and our announcement [here](https://blog.linuxserver.io/2019/02/21/the-lsio-pipeline-project/).

Simply pulling `rogerfar/rdtclient` should retrieve the correct image for your arch, but you can also pull specific arch images via tags.

The architectures supported by this image are:

| Architecture | Tag |
| :----: | --- |
| x86-64 | amd64-latest |
| arm64 | arm64v8-latest |
| armhf | arm32v7-latest |

## Version Tags

This image provides various versions that are available via tags. `latest` tag usually provides the latest stable version. Others are considered under development and caution must be exercised when using them.

| Tag | Description |
| :----: | --- |
| latest | Stable releases |

## Usage

Here are some example snippets to help you get started creating a container.

### docker-compose ([recommended](https://docs.linuxserver.io/general/docker-compose))

Compatible with docker-compose v2 schemas.

```yaml
---
version: '3.3'
services:
  rdtclient:
    image: rogerfar/rdtclient
    container_name: rdtclient
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Europe/London
    volumes:
      - <path to data>:/data/db
      - <path to downloads>:/data/downloads
    logging:
       driver: json-file
       options:
          max-size: 10m
    ports:
      - 6500:6500
    restart: unless-stopped
```

### docker cli

```
docker run -d \
  --name=rdtclient \
  -e PUID=1000 \
  -e PGID=1000 \
  -e TZ=Europe/London \
  -p 6500:6500 \
  -v <path to data>:/data/db \
  -v <path/to/downloads>:/data/downloads \
  --restart unless-stopped \
  rogerfar/rdtclient
```

## Parameters

Container images are configured using parameters passed at runtime (such as those above). These parameters are separated by a colon and indicate `<external>:<internal>` respectively. For example, `-p 8080:80` would expose port `80` from inside the container to be accessible from the host's IP on port `8080` outside the container.

| Parameter | Function |
| :----: | --- |
| `-p 6500` | WebUI |
| `-e PUID=1000` | for UserID - see below for explanation |
| `-e PGID=1000` | for GroupID - see below for explanation |
| `-e TZ=Europe/London` | Specify a timezone to use EG Europe/London. |
| `-v /data/db` | App data. |
| `-v /data/downloads` | Location of downloads on disk. |

## Environment variables from files (Docker secrets)

You can set any environment variable from a file by using a special prepend `FILE__`.

As an example:

```
-e FILE__PASSWORD=/run/secrets/mysecretpassword
```

Will set the environment variable `PASSWORD` based on the contents of the `/run/secrets/mysecretpassword` file.

## Umask for running applications

For all of our images we provide the ability to override the default umask settings for services started within the containers using the optional `-e UMASK=022` setting.
Keep in mind umask is not chmod it subtracts from permissions based on it's value it does not add. Please read up [here](https://en.wikipedia.org/wiki/Umask) before asking for support.

## User / Group Identifiers

When using volumes (`-v` flags) permissions issues can arise between the host OS and the container, we avoid this issue by allowing you to specify the user `PUID` and group `PGID`.

Ensure any volume directories on the host are owned by the same user you specify and any permissions issues will vanish like magic.

In this instance `PUID=1000` and `PGID=1000`, to find yours use `id user` as below:

```
  $ id username
    uid=1000(dockeruser) gid=1000(dockergroup) groups=1000(dockergroup)
```


&nbsp;
## Application Setup

Webui can be found at  `<your-ip>:6500` 

## Support Info

* Shell access whilst the container is running: `docker exec -it rtdclient /bin/bash`
* To monitor the logs of the container in realtime: `docker logs -f rdtclient`
* container version number
  * `docker inspect -f '{{ index .Config.Labels "build_version" }}' rdtclient`
* image version number
  * `docker inspect -f '{{ index .Config.Labels "build_version" }}' rogerfar/rdtclient`

## Updating Info

Most of our images are static, versioned, and require an image update and container recreation to update the app inside. Please consult the [Application Setup](#application-setup) section above to see if it is recommended for the image.

Below are the instructions for updating containers:

### Via Docker Compose
* Update all images: `docker-compose pull`
  * or update a single image: `docker-compose pull rdtclient`
* Let compose update all containers as necessary: `docker-compose up -d`
  * or update a single container: `docker-compose up -d rdtclient`
* You can also remove the old dangling images: `docker image prune`

### Via Docker Run
* Update the image: `docker pull rogerfar/rdtclient`
* Stop the running container: `docker stop rdtclient`
* Delete the container: `docker rm rdtclient`
* Recreate a new container with the same docker run parameters as instructed above (if mapped correctly to a host folder, your `/data` folder and settings will be preserved)
* You can also remove the old dangling images: `docker image prune`

### Via Watchtower auto-updater (only use if you don't remember the original parameters)
* Pull the latest image at its tag and replace it with the same env variables in one run:
  ```
  docker run --rm \
  -v /var/run/docker.sock:/var/run/docker.sock \
  containrrr/watchtower \
  --run-once rtdclient
  ```
* You can also remove the old dangling images: `docker image prune`

**Note:** We do not endorse the use of Watchtower as a solution to automated updates of existing Docker containers. In fact we generally discourage automated updates. However, this is a useful tool for one-time manual updates of containers where you have forgotten the original parameters. In the long term, we highly recommend using [Docker Compose](https://docs.linuxserver.io/general/docker-compose).

### Image Update Notifications - Diun (Docker Image Update Notifier)
* We recommend [Diun](https://crazymax.dev/diun/) for update notifications. Other tools that automatically update containers unattended are not recommended or supported.

## Building locally

If you want to make local modifications to these images for development purposes or just to customize the logic:
```
git clone https://github.com/ravensorb/docker-rdtclient.git
cd docker-rdtclient
docker build \
  --no-cache \
  --pull \
  -t rogerfar/rtd-client:latest .
```

The ARM variants can be built on x86_64 hardware using `multiarch/qemu-user-static`
```
docker run --rm --privileged multiarch/qemu-user-static:register --reset
```

Once registered you can define the dockerfile to use with `-f Dockerfile.aarch64`.
