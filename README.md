# Real-Debrid Torrent Client

This is a web interface to manage your torrents on Real-Debrid. It supports the following features:

- Add new torrents through magnet or files
- Download all files from Real Debrid to your local machine automatically
- Unpack all files when finished downloading
- Implements a fake qBittorrent API so you can hook up other applications like Sonarr or Couchpotato.
- Built with Angular 9 and .NET 5

**You will need a Premium service at Real-Debrid!**

## Docker Run

```console
docker run --cap-add=NET_ADMIN -d \
              -v /your/storage/path/:/data/downloads \
              --log-driver json-file \
              --log-opt max-size=10m \
              -p 6500:6500 \
              rogerfar/rdtclient
```

Replace `/your/storage/path/` with your local path to download files to. For Windows i.e. `C:/Downloads`.

## Windows Service Installation

1. Make sure you have the .NET 5.0.1+ runtime installed from [here](https://dotnet.microsoft.com/download).
1. Unpack the latest release from the releases folder and run `startup.bat`. This will start the application on port 6500.
1. To install as service on Windows, exit the console and run `serviceinstall.bat` as administrator.
1. To change the default port edit the `appsettings.json` file.

## Setup

1. Browse to [http://127.0.0.1:6500](http://127.0.0.1:6500) (or the path of your server).
1. The very first credentials you enter in will be remembered for future logins.
1. Click on `Settings` on the top and enter your Real-Debrid API key (found here: [https://real-debrid.com/apitoken](https://real-debrid.com/apitoken).
1. Change your download path if needed. When using Docker, this path will be the path on your local machine.
1. Save your settings.

## Removing

1. Run `serviceremove.bat` to remove the service and firewall rules.

## Troubleshooting

- If you forgot your logins simply delete the `database.db` and restart the service.

## Connecting Sonarr/Radarr

RdtClient emulates the qBittorrent web protocol and allow applications to use those APIs. This way you can use Sonarr and Radarr to download directly from RealDebrid.

1. Login to Sonarr or Radarr and click `Settings`.
1. Go to the `Download Client` tab and click the plus to add.
1. Click `qBittorrent` in the list.
1. Enter the IP or hostname of the RealDebridClient in the `Host` field.
1. Enter the 6500 in the `Port` field.
1. Enter your Username/Password you setup in step 3 above in the Username/Password field.
1. Leave the other settings as is.
1. Hit `Test` and then `Save` if all is well.
1. Sonarr will now think you have a regular Torrent client hooked up.

Notice: the progress and ETA reported in Sonarr's Activity tab will not be accurate, but it will report the torrent as completed so it can be processed after it is done downloading.

## Build instructions

### Prerequisites

- NodeJS
- NPM
- (optional) Angular CLI
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
