# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.39] - 2023-09-21
### Changed
- Fixed docker build

## [2.0.38] - 2023-09-20
### Added
- Added Symlink downloader to allow the use of rclone.
- Added extra logging.
- Added bulk retry on the index.

### Changed
- Lower password requirements

## [2.0.37] - 2023-08-03
### Changed
- Fixed docker build process.

## [2.0.36] - 2023-08-02
### Changed
- Fixed docker build process and upgraded to Node18.

## [2.0.35] - 2023-08-02
### Removed
- Docker arm32/v7 images because the base image does not support it anymore: https://www.linuxserver.io/blog/a-farewell-to-arm-hf

## [2.0.34] - 2023-08-01
### Changed
- Update docker image to alpine 3.14.

## [2.0.33] - 2023-08-01
### Changed
- Fixed sub folders on Premiumize.
- Fixed serialization errors on Premiumize.
- Filter illegal path and filename characters when setting the download paths.
### Added
- Add the option "Post Download Action" to the Torrent settings popup.
- Add a 2nd "Add Torrent" button on the add torrent page.
- Add the Aria2c downloader to the Docker container and set it as the default downloader when running in docker.

## [2.0.32] - 2023-06-27
### Changed
- Fixed the BaseURL content-length setting.

## [2.0.32] - 2023-06-27
### Changed
- Fixed the BaseURL content-length setting.

## [2.0.31] - 2023-06-11
### Added
- Added setting to set the BaseURL.

### Changed
- Added some logging.

## [2.0.30] - 2023-04-09
### Changed
- Improved the internal downloader bandwidth limiter.

## [2.0.29] - 2023-04-09
### Changed
- Testing Github actions automatic build to Docker.

## [2.0.28] - 2023-04-09
### Changed
- Added link to repository from the version link.

## [2.0.27] - 2023-04-08
### Changed
- Fixed Premiumize selecting of files.
- Updated internal downloader to report download speeds better.

## [2.0.26] - 2023-03-30
### Changed
- Add some logging for Premiumize to better understand errors.
- Changed Premiumize to download each file individually instead of creating a zip first.
- Swapped the internal downloader for a new version.
- Fixed issue when switching providers and it not switching over properly.

## [2.0.25] - 2023-03-17
### Changed
- Fixed docker run issues.

## [2.0.24] - 2023-03-16
### Changed
- Fixed docker run issues.

## [2.0.23] - 2023-03-15
### Changed
- Fixed docker build issues.

## [2.0.22] - 2023-03-11
### Added
- Add support for Premiumize

## [2.0.21] - 2023-03-09
### Changed
- Fixed docker build by pinning the .NET version to LTS.

## [2.0.20] - 2023-03-08
### Added
- Add support for multi-level unpacking.
- Add settings to specify the Error and Processed paths for the watch folders.
- Option to disable authentication completely.

### Changed
- Removed the Simple Downloader and replaced it with https://github.com/bezzad/Downloader as the Internal Downloader.
- Fixed setting the Base Href for sub folder hosting.

## [2.0.19] - 2022-10-18
### Changed
- Changed the AllDebrid provider to use HTTPS instead of HTTP.

## [2.0.18] - 2022-10-18
### Added
- Added the option to bulk delete torrents, thanks kanazaca!
- Added option to remove the torrent only from the client after downloads are completed.
- Added option to change the category of an existing torrent.
- Added option to not download files to the host.
### Changed
- If a watched file gives an error when adding, move it to an error folder.
  
## [2.0.17] - 2022-05-24
### Changed
- Fixed issue with some settings not saving.

## [2.0.16] - 2022-05-24
### Changed
- Fixed MacOS pre-fill on the login screen.

## [2.0.15] - 2022-05-15
### Changed
- Remove settings for Finish Action and Category for the qBittorrent integration, these should always be set to None and the category comes from the integration.

## [2.0.14] - 2022-05-14
### Changed
- Fixed Windows Service issue
- For service users: the appsettings.json is slightly changed: HostUrl is now Port. Important if you used a non standard (6500) port.

## [2.0.13] - 2022-05-14
### Changed
- Rewrote the settings storage. Added more settings to set defaults for importing.
- Fixed filtering of torrents when a category is passed to the TorrentsInfo endpoint, fixing Radarr/Sonarr integrations.
### Added
- Added settings to control the timeout and polling interval to RealDebrid/AllDebrid.

## [2.0.12] - 2022-03-20
### Changed
- Fixed the AllDebrid client.

## [2.0.11] - 2022-03-19
### Changed
- Fixed the "Progress" for the AllDebrid client, thanks @23doors.

## [2.0.10] - 2022-03-19
### Added
- When changing the download speed setting for the simple downloader it will apply the setting to active downloads.
- Add running of external applications when the torrent is finished.
### Changed
- Fixed deserialization of the availability check for RealDebrid.
- Fixed the simple downloader download limiter.

## [2.0.9] - 2022-03-12
### Changed
- Updated packages, added logging to the RD and AD providers when serialization fails.

## [2.0.8] - 2022-02-28
### Changed
- Fixed issue with AllDebrid sometimes returning NULL links.

## [2.0.7] - 2022-02-06
### Changed
- Added setting to set the category when a torrent is imported from RealDebrid or other provider.

## [2.0.6] - 2022-02-06
### Added
- Added setting to automatically delete torrents in the state of error after a certain amount of time.
- Added lifetime setting to automatically expire torrents after a certain amount of time.

## [2.0.5] - 2022-01-11
### Changed
- Updated AllDebrid provider to fix issue with ID's not being a number.

## [2.0.4] - 2022-01-08
### Changed
- Fixed bug where the QBittorrent API didn't report the error state correctly when an error ocurred in the debrid provider.

## [2.0.3] - 2022-01-02
### Changed
- Fixed automatic adding of AllDebrid torrents.
### Added
- Added update notification.

## [2.0.2] - 2021-11-24
### Changed
- Fixed update timer for providers.

## [2.0.1] - 2021-11-24
### Changed
- Fixed potential issue with the Real-Debrid mapper.
- Fixed serialization issue for AllDebrid.

## [2.0.0] - 2021-11-21
### Changed
- Update projects to .NET6 and Angular 13.
### Added
- Added setting to automatically import torrents from RealDebrid / AllDebrid.
- Added setting to automatically delete torrents from RealDebridClient when they have been removed from RealDebrid / AllDebrid.

## [1.9.8] - 2021-10-30
### Added
- Add speed limit setting on the Simple downloader.

## [1.9.7] - 2021-10-30
### Added
- Add AllDebrid support.

## [1.9.6] - 2021-10-30
### Added
- Improved handling of errors on the torrent itself
- Added retry counts when adding torrents
- Added retry counts on the Sonarr/Radarr settings page

## [1.9.5] - 2021-10-28
### Changed
- Fixed issues with the simple downloader.
- Fixed issue with retrying downloads.

### Added
- Added torrent uploading status from Real Debrid.
- Restored removing of torrents when deleted from Real Debrid.

## [1.9.4] - 2021-10-28
### Changed
- Fixed issues retrying torrents after multiple failed downloads in large torrents.

## [1.9.3] - 2021-10-27
### Changed
- Fixed issue with the unpack queue.

## [1.9.2] - 2021-10-27
### Changed
- Fixed issue where not the correct torrent was used for a download.

## [1.9.1] - 2021-10-27
### Added
- Added automatic torrent retrying when RealDebrid reports an error on the torrent.

### Changed
- Changed how Aria2 is polled, this will result in less RPC calls for a large amount of torrents.
- Add more and better logging for the torrent runner in Debug.
- Add Aria2 checks to see if links get added properly.
- Update Aria2.NET and RD.NET, added automatic retrying in case of server failures.

## [1.9.0] - 2021-10-24
### Added
- Add priorty for torrents. You can set the priority when adding a new torrent. When added from Sonarr/Radarr it will assume no priority.
- Added support for the Sonarr/Radarr/qBittorrent setPrio command. It will set the priority of a torrent to 1.
- Added support for the Sonarr/Radarr/qBittorrent pause and resume commands. This only works with the Aria2 downloader.

### Changed
- Fixed issues when downloads get deleted when they are still being checked.
- Improved timings and retrying of adding of downloads to Aria2. Will now try 5 times before failing.

## [1.8.9] - 2021-10-23
### Changed
- Add delays between adding downloads to Aria2 to avoid Aria2 going down when adding a large amount of downloads.

## [1.8.8] - 2021-10-21
### Changed
- Fixed starting downloads when RealDebrid reports ghost links in torrents.

## [1.8.7] - 2021-10-11
### Added
- Add Aria2 test connection button.
- Add full torrent retry mechanism, by default it will now retry the torrent 2 times when a download fails for more than 3 times.

### Changed
- Improved Aria2 download handling.

## [1.8.6] - 2021-10-09
### Added
- Experimental support for a Aria2 download client. Check the readme for usage.

### Changed
- Fixed potential error when sonarr is querying and the torrent isn't added to RealDebrid yet.
- Fixed interface randomly stop updating.
- Upgrade dependencies.

## [1.8.5] - 2021-10-07
### Changed
- Fixed issue where deleting a torrent could error out.

## [1.8.4] - 2021-08-05
### Changed
- Changed the default timeout for Real-Debrid communication to 10 seconds instead of 100 seconds.

## [1.8.3] - 2021-08-05
### Changed
- Fixed potential issue with duplicates categories.

## [1.8.2] - 2021-08-02
### Changed
- Fixed issue with starting downloads.

## [1.8.1] - 2021-07-31
### Changed
- Fixed issue where downloads were hanging the full interface.

## [1.8.0] - 2021-07-18
### Added
- Fixed support for categories. They are now saved persistently in the database.
- Added new "settings" page.
- Added new "add new" page. Added more options when manually adding a torrent, including the ability to manually select files.
- Added dedicated torrent pages with more information when selecting a torrent on the main page.
- Add ability to retry individual downloads.

### Changed
- Fixed enter key on the login and setup screen.
- Fixed an issue with selecting files and getting the links. This process could take a long time and would hang the client while waiting for a response.

### Removed
- Removed the retry and delete button from the main page and moved them to the torrent page.
- Removed the ability to retry all downloads.

## [1.7.8] - 2021-06-17
### Changed
- Fixed issue for real this time with a broken response from RDT when the available files returns in a format different than normal.

## [1.7.7] - 2021-06-09
### Changed
- Fixed some issues with download error handling.
- Fixed issue where files aren't always properly selected.

## [1.7.6] - 2021-06-05
### Changed
- Fixed build for Raspberry PI.

## [1.7.5] - 2021-06-05
### Changed
- Reduced the frequency of database reads and writes by adding caches for settings and torrents.
- Updated dependency of RD.NET to improve serialization error reporting.
- Changed how the base href is determined to support path proxies.
- Fixed issue with handling of renamed torrents.

## [1.7.4] - 2021-04-22
### Changed
- Changed how the docker is built.

## [1.7.3] - 2021-04-19
### Changed
- Fixed issue (hopefully) where sometimes the name of the torrent would change.

## [1.7.2] - 2021-04-14
### Changed
- Fixed issue where sometimes the content path is not recognized by Sonarr. 
- Fixed reporting errors when a RD torrent errors.

## [1.7.1] - 2021-04-01
### Added
- Add Update.ps1 to automatically download the latest version from Github for Windows users.

## [1.7.0] - 2021-04-01
### Added
- Add automatic retry functionality for downloads. It will retry downloading a file after an error 3 times.
### Changed
- Fixed an issue downloads sometimes getting added multiple times.

## [1.6.3] - 2021-03-15
### Changed
- Fixed a bug where it sometimes could add a download multiple times when the RD torrent was in a certain state.

## [1.6.2] - 2021-03-13
### Added
- Add a retry button in the interface. Giving the possibility to re-download the torrent in RealDebrid or re-download the files locally.
### Changed
- Fixed some issues with deleting files and the files being in use.
- Fixed an issue with some torrents not downloading when finished.
### Removed
- Removed the "Download" and "Unpack" buttons from the interface.

## [1.6.1] - 2021-03-12
### Changed
- Fixed a bug in the torrent runner where it could download the same file multiple times.

## [1.6.0] - 2021-03-10
### Added
- Add an option to see which files are available on Real-Debrid on the add torrent modal.
### Changed
- Update the unrestrict process to only unrestrict links when a download is started instead of when the torrent is added.
### Removed
- Removed the "Auto download" and "Auto unpack" options, they are now always enabled by default.

## [1.5.6] - 2021-02-14
### Changed
- Updated all packages
- Updated the multi downloader, this should fix the insufficient disk space issue.

## [1.5.5] - 2021-02-12
### Added
- You can now enable debug logging through the setting modal. Debug will give you a lot of information, but also the best picture of what's going on. Default log level is warning to avoid creating big log files.

### Changed
- Fixed the bug where the unpack process would halt if there were downloads pending.

## [1.5.4] - 2021-02-11
### Changed
- Fixed an issue where auto extracting wasn't working.

## [1.5.3] - 2021-02-10
### Changed
- Fixed issue with selecting files when all files were filtered out.

## [1.5.2] - 2021-02-09
### Added
- Add ProxyServer to the Multipart download settings.

### Changed
- Fixed issue with RealDebrid API not returning a proper list of available files.

## [1.5.1] - 2021-02-06

### Changed
- Fixed bug with the OnlyDownloadAvailable files setting not showing correctly enabled in the setting modal.

## [1.5.0] - 2021-02-05
### Added
- Added a new setting to only download files that have been downloaded on Real Debrid, this will make downloading relevant files faster.
- Re-added the service-install.bat and service-remove.bat files to whoever wants to run as a service.

## [1.4.0] - 2021-01-13
### Added
- This release will now support the Docker image. You can still run it as a service as before.

### Changed
- Lots of changes, but the most notable is that the database is stored an a persistent storage on your host, configured through the docker file. This does mean that any torrents you are currently running will be removed from RDT.
- Sonarr and Radarr support have been greatly improved, especially the version 3 releases.
- The download/unpack queue process has been rewritten to be more robust.

## [1.3.0] - 2020-12-01
### Changed
- Sonarr and Radarr compatibility broke due to the missing files endpint, thank you @alexmckenley for adding the endpoint!

## [1.2.0] - 2020-10-05
### Changed
- Fixed small issues when adding torrents and not loading meta data.

## [1.1.0] - 2020-05-16
### Changed
- Small bug fixes

### Changed
- test

### Removed
- nothing

## [1.0.0] - 2020-04-11
### Added
- First release
- Add unraring progress and default auto download / auto remove options.

[Unreleased]: https://github.com/rogerfar/rdt-client/compare/1.5.5...HEAD
[1.5.5]: https://github.com/rogerfar/rdt-client/releases/tag/1.5.5
[1.5.4]: https://github.com/rogerfar/rdt-client/releases/tag/1.5.4
[1.5.3]: https://github.com/rogerfar/rdt-client/releases/tag/1.5.3
[1.5.2]: https://github.com/rogerfar/rdt-client/releases/tag/1.5.2
[1.5.1]: https://github.com/rogerfar/rdt-client/releases/tag/1.5.1
[1.5.0]: https://github.com/rogerfar/rdt-client/releases/tag/1.5
[1.4.0]: https://github.com/rogerfar/rdt-client/releases/tag/1.4
[1.3.0]: https://github.com/rogerfar/rdt-client/releases/tag/1.3
[1.2.0]: https://github.com/rogerfar/rdt-client/releases/tag/1.2
[1.1.0]: https://github.com/rogerfar/rdt-client/releases/tag/1.1
[1.0.0]: https://github.com/rogerfar/rdt-client/releases/tag/v1.0