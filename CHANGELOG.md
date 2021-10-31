# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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