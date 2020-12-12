# Real-Debrid Torrent Client

This is a web interface to manage your torrents on Real-Debrid. It supports the following features:

- Add new torrents through magnet or files
- Download all files from Real Debrid to your local machine automatically
- Unpack all files when finished downloading
- Implements a fake qBittorrent API so you can hook up other applications like Sonarr or Couchpotato.
- Built with Angular 9 and .NET 5

**You will need a Premium service at Real-Debrid!**

## Installation

### Docker Installation

### Windows Service Installation

1. Make sure you have the .NET 5.0.1+ runtime installed from [here](https://dotnet.microsoft.com/download).
1. Unpack the latest release from the releases folder and run `startup.bat`. This will start the application on port 6500.
1. Browse to http://127.0.0.1:6500
1. The very first credentials you enter in will be remembered for future logins.
1. Click on Settings on the top and enter your Real-Debrid API key.
1. Change your download path if needed.
1. To install as service on Windows, exit the console and run `serviceinstall.bat` as administrator.
1. To change the default port edit the `appsettings.json` file.

## Removing

1. Run `serviceremove.bat` to remove the service and firewall rules.

## Troubleshooting

- If you forgot your logins simply delete the `database.db` and restart the service.

## Connecting Sonarr/Radarr

1. Login to Sonarr or Radarr and click Settings
1. Go to the Download Client tab and click the plus to add
1. Click "qBittorrent" in the list
1. Enter the IP or hostname of the RealDebridClient in the Host field
1. Enter the port 6500 in the Port field
1. Enter your Username/Password you setup in step 3 above in the Username/Password field.
1. Leave the other settings as is.
1. Hit Test and then Save if all is well.
1. Sonarr will now think you have a regular Torrent client hooked up.

Notice: the progress and ETA reported in Sonarr's Activity tab will not be accurate, but it will report the torrent as completed so it can be processed after it is done downloading.

## Build instructions

1. Open the client folder project in VS Code and run `npm install`
2. To debug run `ng serve`, to build run `ng build --prod`
3. Open the Visual Studio 2019 project `RdtClient.sln` and `Publish`
4. The result is found in `\rdt-client\Publish`
