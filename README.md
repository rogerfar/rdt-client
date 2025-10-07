# Real-Debrid Torrent Client

This is a web interface to manage your torrents on Real-Debrid, AllDebrid, Premiumize TorBox or DebridLink. It supports the following features:

- Add new torrents through magnets or files
- Download all files from Real-Debrid, AllDebrid, Premiumize or TorBox to your local machine automatically
- Unpack all files when finished downloading
- Implements a fake qBittorrent API so you can hook up other applications like Sonarr, Radarr or Couchpotato.
- Built with Angular 15 and .NET 9

**You will need a Premium service at Real-Debrid, AllDebrid, Premiumize or Torbox!**

[Click here to sign up for Real-Debrid.](https://real-debrid.com/?id=1348683)

[Click here to sign up for AllDebrid.](https://alldebrid.com/?uid=2v91l)

[Click here to sign up for Premiumize.](https://www.premiumize.me/)

[Click here to sign up for TorBox.](https://torbox.app/subscription?referral=3d25018e-f30d-4c4b-a714-48c04bc76765)

[Click here to sign up for DebridLink.](https://debrid-link.fr/id/6duif)

<sub>(referal links so I can get a few free premium days)</sub>

## Docker Setup

Please see our separate Docker setup Read Me.

[Readme for Docker](README-DOCKER.md)

## Run as a Service

Instead of running in Docker you can install it as a service in Windows or Linux.

## Windows Service

1. Make sure you have the **ASP.NET Core Runtime 9.0.0** installed: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Get the latest zip file from the Releases page and extract it to your host.
3. Open the `appsettings.json` file and replace the `LogLevel` `Path` to a path on your host.
4. In `appsettings.json` replace the `Database` `Path` to a path on your host.
5. When using Windows paths, make sure to escape the slashes. For example: `D:\\RdtClient\\db\\rdtclient.db`
6. Do one of these:
	* Run `RdtClient.Web.exe` to start the client.
 	* Run `service-install.bat` to install the client as a service. This will install `RdtClient.Web.exe` as a service which make the client start in the backgorund when the computer starts. (You probably want to do this if you are going to use this with Sonarr etc...)

## Linux Service

Instead of running in Docker you can install it as a service in Linux.

1. Install .NET: [https://docs.microsoft.com/en-us/dotnet/core/install/linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

    Ubuntu 20.04 example:  
    ```wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb```  
    
    ```sudo dpkg -i packages-microsoft-prod.deb```  

    ```rm packages-microsoft-prod.deb```  

    ```sudo apt-get update && sudo apt-get install -y dotnet-sdk-9.0```  

2. Get latest archive from [releases](https://github.com/rogerfar/rdt-client/releases):  
```wget <zip_url>```
3. Extract to path of your choice (~/rtdc in this example):  
```unzip RealDebridClient.zip -d ~/rdtc && cd ~/rdtc```
4. In appsettings.json replace the Database Path to a path on your host. Any directories in path must already exist. Or just remove "/data/db/" for ease.
5. Test rdt client runs ok:  
```dotnet RdtClient.Web.dll```   
navigate to http://<ipaddress>:6500, if all is good then we'll create a service
6. Create a service (systemd in this example):  
```sudo nano /etc/systemd/system/rdtc.service```  

    paste in this service file content and edit path of your directory:

    ```
    [Unit]
    Description=RdtClient Service

    [Service]

    WorkingDirectory=/home/<username>/rdtc
    ExecStart=/usr/bin/dotnet RdtClient.Web.dll
    SyslogIdentifier=RdtClient
    User=<username>

    [Install]
    WantedBy=multi-user.target
    ```

7. enable and start the service:   
```sudo systemctl daemon-reload```  
```sudo systemctl enable rdtc```  
```sudo systemctl start rdtc```  

## Proxmox LXC

If you use Proxmox for your homelab, you can run rdt-client in a linux container (LXC), check it here:
[https://tteck.github.io/Proxmox/](https://tteck.github.io/Proxmox/) 

## Setup

### First Login

1. Browse to [http://127.0.0.1:6500](http://127.0.0.1:6500) (or the path of your server).
1. The very first credentials you enter in will be remembered for future logins.
1. Click on `Settings` on the top and enter your Real-Debrid API key (found here: [https://real-debrid.com/apitoken](https://real-debrid.com/apitoken).
1. If you are using docker then the `Download path` setting needs to be the same as in your docker file mapping. By default this is `/data/downloads`. If you are using Windows, this is a path on your host.
1. Same goes for `Mapped path`, but this is the destination path from your docker mapping. This is a path on your host. For Windows, this will most likely be the same as the `Download path`.
1. Save your settings.

### Download Clients

Currently there 4 available download clients:

#### Bezzad Downloader

This [downloader](https://github.com/bezzad/Downloader) can be used to download files in parallel and with multiple chunks.

It has the following options:

- Download speed (in MB/s): This number indicates the speed in MB/s per download over all parallel downloads and chunks.
- Parallel connections per download: This number indicates how many parallel it will use per download. This can increase speed, recommended is no more than 8.
- Parallel chunks per download: This number indicates in how many chunks each download is split, recommended is no more than 8.
- Connection Timeout: This number indicates the timeout in milliseconds before a download chunk times out. It will retry each chunk 5 times before completely failing.

#### Aria2c downloader

This will use an external Aria2c downloader client. You will need to install this client yourself on your host, it is not included in the docker image.

It has the following options:

- Url: The full URL to your Aria2c service. This must end in /jsonrpc. A standard path is `http://192.168.10.2:6800/jsonrpc`.
- Secret: Optional secret to connecto to your Aria2c service.

If Aria2c is selected, none of the above options for `Internal Downloader` are used, you have to configure Aria2c manually.

#### Symlink downloader

Symlink downloader requires a rclone mount to be mounted into your filesystem. Be sure to keep the exact path to mounted files in other apps exactly
the same as used by rdt-client. Otherwise the symlinks wont resolve the file its trying to point to.

If the mount path folder cant be found the client wont start downloading anything.

Required configuration:
- Post Download Action = DO NOT SELECT REMOVE FROM PROVIDER
- Rclone mount path = /PATH_TO_YOUR_RCLONE_MOUNT/torrents/

Suggested configuration:
- Automatic retry downloads > 3

#### Synology Download Station

The Synology Download Station downloader uses an external Download Station server. You will need to set this up yourself.

It has the following options:

- Url: The URL to the Synology DownloadStation. A common URL is `http://127.0.0.1:5000`
- Username: The username to use when connecting to the Synology DownloadStation.
- Password: The password to use when connecting to the Synology DownloadStation.
- Download Path: The root path to download the file on the Synology DownloadStation host. If left empty, the default path configured on your Download Station server will be used.

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

### Running within a folder

By default the application runs in the root of your hosted address (i.e. https://rdt.myserver.com/), but if you want to run it as a relative folder (i.e. https://myserver.com/rdt) you will have to change the `BasePath` setting in the `appsettings.json` file. You can set the `BASE_PATH` environment variable for docker enviroments.

## Build instructions

### Prerequisites

- NodeJS
- NPM
- Angular CLI
- .NET 9
- Visual Studio 2022
- (optional) Resharper

1. Open the client folder project in VS Code and run `npm install`.
1. To debug run `ng serve`, to build run `ng build -c production`.
1. Open the Visual Studio 2019 project `RdtClient.sln` and `Publish` the `RdtClient.Web` to the given `PublishFolder` target.
1. When debugging, make sure to run `RdtClient.Web.dll` and not `IISExpress`.
1. The result is found in `Publish`.

## Build docker container

1. In the root of the project run `docker build --tag rdtclient .`
1. To create the docker container run `docker run --publish 6500:6500 --detach --name rdtclientdev rdtclient:latest`
1. To stop: `docker stop rdtclient`
1. To remove: `docker rm rdtclient`
1. Or use `docker-build.bat`

## Misc Install Notes

### Rootless Podman, Linux Host, and CIFS Connections

RDT Client read and write permission tests fail if the CIFS connection is not setup properly, despite permissions working inspection.  In the Web GUI, it will report access denied, and in the log file you will see exceptions like this ([dotnet issue](https://github.com/dotnet/runtime/issues/42790)): 
```
System.IO.IOException: Permission denied
```
The **nobrl** has to be specified in your CIFS connection - [man page](https://linux.die.net/man/8/mount.cifs). 
Example: ```Options=_netdev,credentials=/etc/samba/credentials/600file,rw,uid=SUBUID,gid=SBUGID,nobrl,file_mode=0770,dir_mode=0770,noperm```
