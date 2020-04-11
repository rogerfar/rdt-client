# Real-Debrid Torrent Client

This is a web interface to manage your torrents on Real-Debrid. It supports the following features:

-   Add new torrents through magnet or files
-   Download all files from Real Debrid to your local machine automatically
-   Unpack all files when finished downloading
-   Implements a fake qBittorrent API so you can hook up other applications like Sonarr or Couchpotato.
-   Build with Angular 9 and .NET Core 3.1

**You will need a Premium service at Real-Debrid!**

## Installation

1. Unpack the latest release from the releases folder and run `startup.bat`. This will start the application on port 6500.
2. Browse to http://127.0.0.1:6500
3. The very first credentials you type in will be remembered for future logins.
4. Click on Settings on the top and enter your Real-Debrid API key.
5. Change your download path if needed.
6. To install as service on Windows, exit the console and run `serviceinstall.bat` as administrator.

## Troubleshooting

-   If you forgot your logins simply delete the `database.db` and restart the service.

## Connecting Sonarr

1. Login to Sonarr and click Settings
2. Go to the Download Client tab and click the plus to add
3. Click "qBittorrent" in the list
4. Enter the IP or hostname of the RealDebridClient in the Host field
5. Enter the port 6500 in the Port field
6. Enter your Username/Password you setup in step 3 above in the Username/Password field.
7. Leave the other settings as is.
8. Hit Test and then Save if all is well.
9. Sonarr will now think you have a regular Torrent client hooked up.

Notice: the progress and ETA reported in Sonarr's Activity tab will not be very accurate, but it will report the torrent as completed so it can be processed by Sonarr when done downloading.

## Build instructions

1. Open the client folder project in VS Code and run `npm install`
2. To debug run `ng serve`, to build run `ng build --prod`
3. Open the Visual Studio 2019 project `RdtClient.sln` and `Publish`
4. The result is found in `\rdt-client\server\RdtClient.Web\bin\Release\netcoreapp3.1\publish`
