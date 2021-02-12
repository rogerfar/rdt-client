# Real-Debrid Torrent Client

This is a web interface to manage your torrents on Real-Debrid. It supports the following features:

- Add new torrents through magnets or files
- Download all files from Real Debrid to your local machine automatically
- Unpack all files when finished downloading
- Implements a fake qBittorrent API so you can hook up other applications like Sonarr or Couchpotato.
- Built with Angular 11 and .NET 5

**You will need a Premium service at Real-Debrid!**

[Click here to sign up (referal link so I can get a few free premium days).](https://real-debrid.com/?id=1348683)

## Docker Setup

You can run the docker container on Windows, Linux. To get started either use _Docker Run_ or _Docker Compose_.

### Docker Run

```console
docker run --cap-add=NET_ADMIN -d \
              -v /your/download/path/:/data/downloads \
              -v /your/storage/path/:/data/db \
              --log-driver json-file \
              --log-opt max-size=10m \
              -p 6500:6500 \
			  --name rdtclient \
              rogerfar/rdtclient:latest
```

Replace `/your/download/path/` with your local path to download files to. For Windows i.e. `C:/Downloads`.
Replace `/your/storage/path/` with your local path to store persistent database and log files in. For Windows i.e. `C:/Docker/rdt-client`.

### Docker Compose

You can use the provided docker compose to run:

```yaml
version: '3.3'
services:
    rdtclient:
        container_name: rdtclient
        volumes:
            - 'D:/Downloads/:/data/downloads'
            - 'D:/Docker/rdt-client/:/data/db'
        image: rogerfar/rdtclient
        restart: always
        logging:
            driver: json-file
            options:
                max-size: 10m
        ports:
            - '6500:6500'
```

And to run:

```console
docker-compose up -d
```

Replace the paths in `volumes` as in the above step.

## Windows Service

Instead of running in Docker you can install it as a service in Windows or Linux (not tested).

1. Get the latest zip file from the Releases page and extract it to your host.
1. Open the `appsettings.json` file and replace the `LogLevel` `Path` to a path on your host.
1. In `appsettings.json` replace the `Database` `Path` to a path on your host.
1. When using Windows paths, make sure to escape the slashes. For example: `D:\\RdtClient\\db\\rdtclient.db`

## Setup

### First Login

1. Browse to [http://127.0.0.1:6500](http://127.0.0.1:6500) (or the path of your server).
1. The very first credentials you enter in will be remembered for future logins.
1. Click on `Settings` on the top and enter your Real-Debrid API key (found here: [https://real-debrid.com/apitoken](https://real-debrid.com/apitoken).
1. If you are using docker then the `Download path` setting needs to be the same as in your docker file mapping. By default this is `/data/downloads`. If you are using Windows, this is a path on your host.
1. Same goes for `Mapped path`, but this is the destination path from your docker mapping. This is a path on your host. For Windows, this will most likely be the same as the `Download path`.
1. Save your settings.

### Download Clients

Currently there 2 available download clients:

#### Simple Downloader

This is a simple 1 connection only download manager. It uses less resources than the multi-part downloader. It downloads straight to the download path.

It has the following options:

- Maximum parallel downloads: This number indicates how many completed torrents from Real Debrid can be downloaded at the same time. On low powered systems it is recommended to keep this number low.

#### Multi Part Downloader

This [downloader](https://github.com/bezzad/Downloader) as more options and such uses more resources (memory, CPU) to download files. Recommended more powerful systems.

It has the following options:

- Temp Download path: Set this path to where the downloader temporarily stores chunks. This path can be an internal path in Docker (i.e. `/data/temp`) but make sure you have enough disk space to complete the whole download. When all chunks are completed the completed file is copied to your download folder.
- Maximum parallel downloads: This number indicates how many completed torrents from Real Debrid can be downloaded at the same time.
- Parallel connections per download: This number indicates how many threads/connections/parts/chunks it will use per download. This can increase speed, recommended is no more than 8.
- Download speed (in MB/s): This number indicates the speed in MB/s per download. If you set this to 10 and `Maximum parallel downloads` to 2, you can download with a maximum of 20MB/s.

### Troubleshooting

- If you forgot your logins simply delete the `rdtclient.db` and restart the service.
- A log file is written to your persistent path as `rdtclient.log`. When you run into issues please change the loglevel in your docker script to `Debug`.

### Connecting Sonarr/Radarr

RdtClient emulates the qBittorrent web protocol and allow applications to use those APIs. This way you can use Sonarr and Radarr to download directly from RealDebrid.

1. Login to Sonarr or Radarr and click `Settings`.
1. Go to the `Download Client` tab and click the plus to add.
1. Click `qBittorrent` in the list.
1. Enter the IP or hostname of the RealDebridClient in the `Host` field.
1. Enter the 6500 in the `Port` field.
1. Enter your Username/Password you setup above in the Username/Password field.
1. Set the category to `sonarr` for Sonarr or `radarr` for Radarr.
1. Leave the other settings as is.
1. Hit `Test` and then `Save` if all is well.
1. Sonarr will now think you have a regular Torrent client hooked up.

When downloading files it will append the `category` setting in the Sonarr/Radarr Download Client setting. For example if your Remote Path setting is set to `C:\Downloads` and your Sonarr Download Client setting `category` is set to `sonarr` files will be downloaded to `C:\Downloads\sonarr`.

Notice: the progress and ETA reported in Sonarr's Activity tab will not be accurate, but it will report the torrent as completed so it can be processed after it is done downloading.

## Build instructions

### Prerequisites

- NodeJS
- NPM
- Angular CLI
- .NET 5
- Visual Studio 2019
- (optional) Resharper

1. Open the client folder project in VS Code and run `npm install`.
1. To debug run `ng serve`, to build run `ng build --prod`.
1. Open the Visual Studio 2019 project `RdtClient.sln` and `Publish` the `RdtClient.Web` to the given `PublishFolder` target.
1. When debugging, make sure to run `RdtClient.Web.dll` and not `IISExpress`.
1. The result is found in `Publish`.

## Build docker container

1. In the root of the project run `docker build --tag rdtclient .`
1. To create the docker container run `docker run --publish 6500:6500 --detach --name rdtclientdev rdtclient:latest`
1. To stop: `docker stop rdtclient`
1. To remove: `docker rm rdtclient`
1. Or use `docker-build.bat`
