# Real-Debrid Torrent & Usenet Client

This is a web interface to manage your torrents on Real-Debrid, AllDebrid, Premiumize, TorBox, DebridLink or HIGHWAY. It supports the following features:

- Add new torrents through magnets or files
- Add usenet downloads through NZB files (TorBox, Premiumize and HIGHWAY only)
- Add direct downloads from file hosts (Real-Debrid and AllDebrid only), fed to Sonarr/Radarr by an indexer bridge, see [Direct downloads](#direct-downloads-file-hosts)
- Download all files from Real-Debrid, AllDebrid, Premiumize, TorBox or HIGHWAY to your local machine automatically
- Unpack all files when finished downloading
- Implements a fake qBittorrent API so you can hook up other applications like Sonarr, Radarr or Couchpotato.
- Built with Angular 21 and .NET 10

**You will need a Premium service at Real-Debrid, AllDebrid, Premiumize, Torbox or DebridLink, or a HIGHWAY account!**

[Click here to sign up for Real-Debrid.](https://real-debrid.com/?id=1348683)

[Click here to sign up for AllDebrid.](https://alldebrid.com/?uid=2v91l)

[Click here to sign up for Premiumize.](https://www.premiumize.me/)

[Click here to sign up for TorBox.](https://torbox.app/subscription?referral=3d25018e-f30d-4c4b-a714-48c04bc76765)

[Click here to sign up for DebridLink.](https://debrid-link.fr/id/6duif)

[Click here for HIGHWAY.](https://high-way.me/)

<sub>(referal links so I can get a few free premium days)</sub>

## Docker Setup

Please see our separate Docker setup Read Me.

[Readme for Docker](README-DOCKER.md)

## Run as a Service

Instead of running in Docker you can install it as a service in Windows or Linux.

## Windows Service

1. Make sure you have the **ASP.NET Core Runtime 10.0.0** and the **SDK** installed: [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
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

    ```sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0```  

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

- Download speed (in MB/s): Maximum speed in MB/s applied **per concurrent download** (aggregated across that download's chunks), not a global limit. The effective total ceiling is this value multiplied by the "Maximum parallel downloads" setting. For example, 40 MB/s with 2 parallel downloads is roughly 80 MB/s total, and 40 MB/s with 4 parallel downloads is roughly 160 MB/s total, so total throughput can appear higher than the number you set here. Set to 0 for unlimited.
- Parallel connections per download: This number indicates how many parallel it will use per download. This can increase speed, recommended is no more than 8.
- Parallel chunks per download: This number indicates in how many chunks each download is split, recommended is no more than 8.
- Connection Timeout: This number indicates the timeout in milliseconds before a download chunk times out. It will retry each chunk 5 times before completely failing.
- Bind to specific IP: When enabled, the downloader binds outbound connections to the configured local IP address. Useful on hosts with multiple network interfaces.
- Bind IP address: The local IP address to bind to. Must be an IP assigned to an active network adapter on this host.

#### Aria2c downloader

This will use an external Aria2c downloader client. You will need to install this client yourself on your host, it is not included in the docker image.

It has the following options:

- Url: The full URL to your Aria2c service. This must end in /jsonrpc. A standard path is `http://192.168.10.2:6800/jsonrpc`.
- Secret: Optional secret to connecto to your Aria2c service.

If Aria2c is selected, none of the above Bezzad options are used, you have to configure Aria2c manually.

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

The Synology Download Station downloader hands each file to your NAS's Download Station, which downloads it and writes it to disk. The bytes never pass through rdt-client, so its memory stays flat regardless of file size.

Prerequisites on the Synology:

- Install and start the **Download Station** package.
- Use a DSM account that has **both Download Station and File Station permission** (File Station is required so rdt-client can create the per-download destination folder). Disable 2-step verification on this account (the API login is username/password only), or use a dedicated service account.
- Download Station's **Default destination** needs to be set for the new user. Rdt-client sets this account's default destination for you if it doesn't already have one, so you normally don't need to do anything. If that automatic set fails, sign into Download Station *as that account* and set it under **Settings → BT/HTTP/FTP/NZB → Location → Default destination** — otherwise every task that account creates stays stuck in **"Waiting"** (DSM logs `Failed to get default download destination of user [<account>]`).

It has the following options:

- Url: The URL to the Synology DownloadStation. A common URL is `http://127.0.0.1:5000`. From inside a Docker container `127.0.0.1` is the container itself — use the NAS's LAN IP (e.g. `http://192.168.1.50:5000`) and the DSM web port (HTTP `5000` by default).
- Username: The username to use when connecting to the Synology DownloadStation.
- Password: The password to use when connecting to the Synology DownloadStation.
- Download Path: The destination on the Synology, **relative to a shared folder** (e.g. `Media/Downloads/Torrents`) — **not** an absolute path like `/volume1/Media/Downloads/Torrents`. **This must resolve to the exact same physical folder as the general Download path** — see Path mapping below. If left empty, the default Download Station destination is used.

**Path mapping (important).** Download Station runs on the NAS; rdt-client runs in its container. They see the same folder through different mounts, so:

> **The Synology *Download Path* (above) and rdt-client's general *Download path* must resolve to the exact same physical folder on the NAS.**

### HIGHWAY provider

[HIGHWAY](https://high-way.me/) (`high-way.me`) is also a multi hoster that downloads torrents **and** usenet server-side. rdt-client pushes magnets/NZBs to it and pulls the finished files down.

Configuration under `Settings` &rarr; `Provider`:

- **Provider**: `HIGHWAY`
- **API Key**: a token you create once in the [HIGHWAY Center](https://high-way.me/) (log in, open **Sitzungen** &rarr; **API-Token erstellen**). This is a 32-character hex string and is revocable from that same list.

Getting the bytes to disk — two modes:

**A. Symlink + rclone WebDAV mount (recommended).** Mount HIGHWAY's WebDAV and use the **Symlink** download client. Example `rclone.conf`:

```ini
[highway]
type = webdav
url = https://cloud.high-way.me/dav/
vendor = other
user = <highway username>
pass = <highway WebDAV / "usenet" password, or the same 32-hex API token from Settings>
```

```
rclone mount highway: /mnt/highway --vfs-cache-mode full
```

Set **Rclone mount path** to `/mnt/highway/` and set the **Post Torrent Download Action** so it does **not** remove the transfer from the provider. rdt-client symlinks the finished files straight from the mount — no re-download.

**B. Internal / Aria2c (no mount).** rdt-client downloads each file directly using its own signed URL. The usual file filters (include/exclude regex, minimum size) apply. If HIGHWAY hasn't signed per-file it falls back to fetching the whole transfer as one ZIP.

Notes:

- HIGHWAY torrents leech from peers server-side, so expect real `downloading` progress for minutes/hours. Only content HIGHWAY already has is linked instantly.
- When HIGHWAY is at its active-transfer limit a new add goes into HIGHWAY's own queue and reports `queued`; this is normal and it will start automatically.

### Troubleshooting

- If you forgot your logins simply delete the `rdtclient.db` and restart the service.
- A log file is written to your persistent path as `rdtclient.log`. When you run into issues please change the loglevel in your docker script to `Debug`.

### Connecting Sonarr/Radarr

RdtClient emulates the qBittorrent web protocol and allows applications to use those APIs. This way you can use Sonarr and Radarr to download directly from RealDebrid.

#### Torrents

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

#### Usenet/NZB

RdtClient also emulates part of the SABnzbd API so Sonarr and Radarr can add NZB downloads. This requires a provider that supports Usenet/NZB downloads, currently TorBox, Premiumize or HIGHWAY.

1. Login to Sonarr or Radarr and click `Settings`.
1. Go to the `Download Client` tab and click the plus to add.
1. Click `SABnzbd` in the list.
1. Enter the IP or hostname of RdtClient in the `Host` field.
1. Enter `6500` in the `Port` field.
1. Enable `Use SSL` only if you access RdtClient through HTTPS.
1. Leave `URL Base` empty unless RdtClient is configured with a `BasePath`, for example `/rdt`.
1. If RdtClient authentication is enabled, leave `API Key` empty and enter your RdtClient username and password. If your client only supports an API key, enter `{username}:{password}` in `API Key`.
1. If RdtClient authentication is disabled, enter any value in `API Key`, for example `rdtclient`, and leave username/password empty.
1. Set the category to `sonarr` for Sonarr or `radarr` for Radarr.
1. Hit `Test` and then `Save` if all is well.

When importing completed NZB downloads, Sonarr/Radarr must be able to access the path reported by RdtClient. In Docker setups this may require a Remote Path Mapping from the RdtClient download path to the path mounted inside Sonarr/Radarr.

#### Direct downloads (file hosts)

RdtClient can download releases that are published as file-host links (Rapidgator, Nitroflare, DDownload, 1fichier, …) instead of torrents or NZBs. It relies on your debrid provider to unlock the hoster links, so it needs **Real-Debrid or AllDebrid**; other providers do not unlock arbitrary hoster links. It has been tested with AllDebrid.

Sonarr and Radarr cannot search file hosts themselves. You need an *indexer bridge*: a small Newznab-compatible service, specific to the site that lists the releases, that you add to Sonarr/Radarr as an indexer. When Sonarr/Radarr grab a release, the bridge returns a **stand-in NZB** instead of a real one:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<nzb xmlns="http://www.newzbin.com/DTD/2003/nzb">
  <head>
    <meta type="name">Movie.2026.1080p.WEB-DL-GRP</meta>
    <meta type="x-ddl-post">https://example.com/movie-2026-1080p-web-dl-grp/</meta>
    <meta type="x-ddl-source">example</meta>
  </head>
  <file poster="bridge" date="0" subject="Movie.2026.1080p.WEB-DL-GRP">
    <groups><group>alt.binaries.ddl</group></groups>
    <segments><segment bytes="1" number="1">placeholder@ddl.invalid</segment></segments>
  </file>
</nzb>
```

- `x-ddl-post` (required) is the http(s) URL of the release post. It identifies the item: the same post URL always maps to the same item.
- `name` (optional) is the release name; the end of the post URL is used when it is missing.
- `x-ddl-source` (optional) is informational.
- The dummy `<file>` is only there so Sonarr/Radarr accept the NZB. RdtClient never reads it.

Set up the download client exactly as in [Usenet/NZB](#usenetnzb) above (`SABnzbd`, port `6500`, category `sonarr` or `radarr`). Sonarr/Radarr send releases from a Newznab indexer to their usenet client, so the bridge's grabs land here while torrents keep going to the qBittorrent client.

##### Waiting for links

A stand-in NZB creates an item in the **Waiting for links** state. Nothing is downloaded until the hoster links of the post are supplied, in one of three ways:

- **Item page**: paste the links into the *Waiting for links* box. Any text works, links are extracted from it.
- **`/ddl` page**: open `/ddl?post=<post url>#links=<url-encoded links, one per line>`. The page shows the matching item and asks for confirmation. The links are passed in the URL fragment so they never reach the server logs, which makes it convenient for a bookmarklet run on the post page.
- **API**, for automation (the bridge itself can unlock the post and submit the links):

  ```
  POST /Api/Torrents/DdlLinks   {"postUrl": "<post url>", "links": "<text containing the links>"}
                                or {"torrentId": "<item id>", "links": "..."}
  GET  /Api/Torrents/Ddl?postUrl=<post url>      the item for this post, 404 if there is none
  ```

  Authenticate with the session cookie from `POST /Api/Authentication/Login` `{"userName": "...", "password": "..."}`. `DdlLinks` returns `404` while Sonarr/Radarr have not handed the NZB over yet, so retry for a little while after a grab.

Set **Settings → General → Direct download waiting webhook** to a Discord-compatible webhook URL to be notified when an item starts waiting for links.

##### Adding links by hand

Links you already have do not need a post or an indexer: **Add → Direct links**, paste them (any text, every mirror if you like) and optionally give the release a name, else it is taken from the file names. The download starts right away. The same links pasted twice are refused. Retrying such an item adds its links again. The same is available as `POST /Api/Torrents/UploadDdlLinks` `{"links": "...", "name": "...", "torrent": {...}}`, with the `torrent` settings of `UploadNzbLink`.

When the `/ddl` page finds no item waiting for the post, it offers to download its links this way.

##### Which links are downloaded

Posts usually list the same release on several hosts, sometimes packaged differently (one file on one host, split `.partN.rar` archives on another). RdtClient downloads from a single host:

1. Hosts whose archive set is complete come first: a host missing a `.partN.rar` that another host has is passed over.
2. Then the configured host order: **Settings → General → Direct download host preference** (for example `nitroflare,rapidgator`), else the `DDL_HOST_PREFERENCE` environment variable, else `rapidgator,nitroflare,ddownload,1fichier,katfile,fikper`. Entries can be names, domains or aliases (`rg.to`, `nitro.download`).
3. Then the number of links, as a last tie-break.

The other hosts are kept as fallbacks:

- **Per file**: when the same file name exists on another host, it is kept as a mirror of that part and used if the debrid provider refuses the link.
- **Whole host**: when the provider rejects the host itself (for example AllDebrid `LINK_HOST_UNAVAILABLE` or `LINK_HOST_NOT_SUPPORTED`) and no mirror of the file exists, the item switches to the next host's links. This only happens before any download of the item has started, so files of two packagings never mix.

Multi-part RAR archives are extracted once every part is downloaded, then the parts are deleted.

### Running within a folder

By default the application runs in the root of your hosted address (i.e. https://rdt.myserver.com/), but if you want to run it as a relative folder (i.e. https://myserver.com/rdt) you will have to change the `BasePath` setting in the `appsettings.json` file. You can set the `BASE_PATH` environment variable for docker enviroments.

## Build instructions

### Prerequisites

- NodeJS
- NPM
- Angular CLI
- .NET 10
- Visual Studio 2025
- (optional) Resharper

### Dev Container

The repository includes a dev container under `.devcontainer/` for the split development workflow used by this project.

It installs .NET 10 and Node 22, forwards ports `4200` and `6500`, and persists `/data/db` and `/data/downloads` in named volumes so the local SQLite database, logs, and downloads survive container rebuilds.

1. Open the repository in the dev container.
1. In one terminal run `dotnet watch run --project server/RdtClient.Web`.
1. In another terminal run `cd client && npm start`.
1. Open `http://localhost:4200`. The Angular dev server proxies `/Api` and `/hub` to the backend running on `6500`.

1. Open the client folder project in VS Code and run `npm install`.
1. To debug run `ng serve`, to build run `ng build -c production`.
1. Open the Visual Studio 2025 project `RdtClient.sln` and `Publish` the `RdtClient.Web` to the given `PublishFolder` target.
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
