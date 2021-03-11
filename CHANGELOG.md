# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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