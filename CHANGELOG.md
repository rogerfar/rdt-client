# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased

## [2.0.119] - 2025-10-13
### Removed
- Removed internal downloader from the GUI.

## [2.0.118] - 2025-10-06
### Added
- Added some fake qBittorrent API calls for decluttarr.

## [2.0.117] - 2025-10-06
### Removed
- Removed internal downloader and migrated to the Bezzad downloader.

## [2.0.116] - 2025-08-04
### Added
- Added setting to ban certain trackers from being added. Will filter by the torrent source or announcement urls.

### Changed
- Upgraded to Angular 20.

## [2.0.115] - 2025-07-28
### Added
- Added setting to delay the finish action.

### Fixed
- Make sure the Real-Debrid provider times out when trying to add a new torrent.

## [2.0.114] - 2025-06-21
### Added
- Add Select All functionality to the delete dialog in individual torrent screen, thanks @mentalblank
- Add setting to add a list of trackers (from a URL) to every torrent and magnet that's added to rdt-client, thanks @mentalblank

### Changed
- The `User-Agent` header is now set on all requests to debrid providers' APIs. 

## [2.0.113] - 2025-05-22
### Fixed
- Revert Synolog.Api.Client because of breaking changes.

## [2.0.112] - 2025-05-18
### Added
- Add ability to disable the built in unpacking process by setting the "Maximum unpack processes" to 0.

### Changed
- Upgraded Angular to use control flow.

### Fixed
- Fixed dequeing issue.
- Fixed logging handler for ProviderUpdater.

## [2.0.111] - 2025-05-03
### Added
- Added button to register rdt-client as a handler for magnet links on [supported browsers](https://caniuse.com/mdn-api_navigator_registerprotocolhandler_scheme_parameter_magnet).

## [2.0.110] - 2025-04-24
### Fixed
- Fixed build number in the app.

## [2.0.109] - 2025-04-23
### Fixed
- Debrid Queue fixes (don't auto delete queued torrents, handle errors when dequeueing).
- Censor download station password when logging settings at startup.
- Set `HostDownloadAction` when auto-importing torrents.
- Build GitHub release .zip on windows not linux.
- Use arm GitHub Actions runner to build arm docker image.

## [2.0.108] - 2025-04-13
### Fixed
- Fixed websocket UI updating.

## [2.0.107] - 2025-04-13
### Fixed
- Fixed Docker release versioning.

## [2.0.106] - 2025-04-13
### Fixed
- Changed how the GitHub release is created and how the changelog is generated.

## [2.0.105] - 2025-04-13
### Added
- Add feature to limit the amount of torrents that get sent to the provider at the same time.
### Fixed
- Moved the websocket update process to its own background thread to improve UI update consistency.

## [2.0.104] - 2025-04-12
### Fixed
- Update the version number

## [2.0.103] - 2025-04-12
### Added
- Button to select all options when deleting a torrent, thanks @EugeneKallis
- Add setting to ignore update notifications. A notification will appear regardless of this setting if any GitHub Security Advisories are published in this repo.
### Changed
- Download .zip of torrent files from TorBox when possible, thanks @asylumexp
- Users of AllDebrid and RealDebrid will now have no files downloaded when all files are excluded by filters. Before, if all files were excluded, rdt-client would download all the files in the torrent.
- Reduce number of calls to debrid provider API when no torrents need updating
### Fixed
- The dropdown navigation menu on mobile will now close when you navigate to another page
- Long torrent names without spaces will now wrap across lines
### Security
- Require auth to change debrid api key

## [2.0.102] - 2025-03-07
### Changed
- Fixed Angular build for Docker.

## [2.0.101] - 2025-03-07
### Changed
- Fixed Angular build (again).

## [2.0.100] - 2025-03-07
### Changed
- Fixed Angular build.

## [2.0.99] - 2025-03-07
### Security fix
- The Api/Authentication/Update was not protected by authentication, meaning everyone could reset your password and gain access.

### Added
- Set the useragent for the Bezadd downloader to avoid getting blacklisted by Torbox.

### Changed
- Upgraded to Angular 19.
- Upgraded to Torbox 1.5.


## [2.0.98] - 2025-02-16
### Added
- Added unit tests, thanks @Cucumberrbob!

### Changed
- Fixed symlinks for AllDebrid.
- Upgraded DebridLink.fr to the latest version.
- Fixed nested files in the Premiumize Provider.
- Fixed deleting of torrents in the watch folders.

## [2.0.97] - 2025-02-16
### Added
- Added support for DebridLink.fr.

### Fixed
- Fixed for the internal downloader.
- Added a column for torrent add date.
- Upgraded AllDebrid API.

## [2.0.96] - 2025-01-29
### Added
- Added support for the synology download manager.
- Added a column for torrent add date.

### Changed
- Fixed for the Symlink downloader and AllDebrid.
- Fixed setting the downloader when adding a torrent through the GUI.

## [2.0.95] - 2025-01-19
### Added
- Added the /api/v2/transfer/info qBittorrent endpoint.

### Changed
- AllDebrid Symlink path fixes.

## [2.0.94] - 2025-01-05
### Changed
- AllDebrid path fixes.

## [2.0.93] - 2025-01-03
### Changed
- Torbox fixes.

## [2.0.92] - 2024-12-18
### Changed
- Torbox fixes.

## [2.0.91] - 2024-12-11
### Changed
- Torbox fixes.

## [2.0.90] - 2024-12-06
### Changed
- Download individual files from Torbox instead of a zip file.

### Removed
- Removed ability to select instant files from AllDebrid.

## [2.0.89] - 2024-11-24
### Changed
- Disabled selecting of files as Real-Debrid was the only provider that supported that.

## [2.0.88] - 2024-11-24
### Changed
- Catch disabled instant availability endpoint from Real Debrid.

## [2.0.87] - 2024-11-18
### Added
- Torbox support.
- qBittorrent API authentication when no authentication is used.
### Changed
- .NET version changed to .NET 9.
- Changed download limit to be split by active torrents.

## [2.0.86] - 2024-09-03
### Changed
- Add potential fix for BASE_PATH.
- Fixed creation of empty folders.

## [2.0.85] - 2024-09-03
### Changed
- Reverted: Prevent the creation of download folders when using symlink.

## [2.0.84] - 2024-09-02
### Changed
- Replace docker libssl1.1 with libssl3

## [2.0.83] - 2024-09-02
### Changed
- Fixed progress reporting to the qBittorrent API endpoint.
- Prevent the creation of download folders when using symlink.

## [2.0.82] - 2024-09-02
### Changed
- Update packages and docker alpine version.

## [2.0.81] - 2024-07-28
### Changed
- Improved handling of infringed torrents from real debrid.
- Force update of torrent data from real-debrid when no filename is found in the local DB.
- Fixed real-debrid deserialization issue when checking for instant available files.

## [2.0.80] - 2024-07-13
### Changed
- Add rate limiter to retry requests that are rate limited from Real-Debrid.
- Optimize calls to Real-Debrid API when torrents are finished and periodic updates.
- Update to .NET 8.0.7

## [2.0.79] - 2024-06-03
### Changed
- Fixed issue with qBittorrent progress sometimes throwing errors.

## [2.0.78] - 2024-05-04
### Changed
- Fixed Aria2c download path issue when a category is set.

## [2.0.77] - 2024-05-03
### Changed
- Fixed Aria2c download path issue when a category is set.

## [2.0.76] - 2024-05-02
### Changed
- Fixed issues with the qBittorrent endpoint.
- Fixed issue that could crash the torrent runner.

## [2.0.75] - 2024-04-24
### Changed
- Fixed broken recursive symlink searching.

## [2.0.74] - 2024-04-20
### Added
- Added support for symlink recursive searching.

## [2.0.73] - 2024-04-11
### Changed
- Fixed another issue with the symlinker and file resolver.

## [2.0.72] - 2024-04-10
### Changed
- Fixed issue with download speed test when the symlink downloader is selected.

## [2.0.71] - 2024-04-10
### Changed
- Fixed symlink path matching bug.

## [2.0.70] - 2024-04-10
### Added
- Added symlink logging.

## [2.0.69] - 2024-04-09
### Added
- Added sorting to the GUI columns.
### Changed
- Fixed reloading on the /settings and other pages.

## [2.0.68] - 2024-04-09
### Changed
- Base Href middleware fix that throws error when a response is not 200.

## [2.0.67] - 2024-04-09
### Changed
- Symlink fixes.

## [2.0.66] - 2024-04-08
### Changed
- Symlink fixes and improvements.

## [2.0.65] - 2024-04-07
### Added
- Added option to configure the buffersize for the internal downloader.

## [2.0.64] - 2024-04-06
### Added
- Add log level Verbose and add logging for the internal downloader, only works when both log levels are set to Verbose.

### Changed
- Add fixes for the symlink downloader
- Add better indication when a torrent is stalled
- Fixed download client selection on the torrents

## [2.0.63] - 2024-03-05
### Changed
- When Sonarr/Radarr requests a torrent to be deleted, and its files too, then delete those files instead of ingoring it.

## [2.0.62] - 2024-02-17
### Changed
- Fixed reporting a torrent as error when some downloads have failed but still need to be retried.
- Fixed issue where downloads could get started over and over.

## [2.0.61] - 2024-01-21
### Added
- Added setting to include or exclude files based on a given regex.
- Add logging.

## [2.0.60] - 2024-01-21
### Changed
- Fixed bug where downloads could get stuck in active state while deleted.
- Use the RD folder structure when downloading a file.

## [2.0.59] - 2024-01-21
### Changed
- Added the simple downloader back and moved the current internal downloader to a new setting.

## [2.0.58] - 2024-01-10
### Changed
- Don't automatically count the chunk count.

## [2.0.57] - 2024-01-09
### Changed
- Fixed symlink retry.

## [2.0.56] - 2024-01-07
### Changed
- Add retry mechanism for the downloaders.

## [2.0.55] - 2024-01-07
### Changed
- Tweaked the internal downloader to prevent memory issues.
- Add retry mechanism for the symlink downloader.

## [2.0.54] - 2024-01-07
### Changed
- Added some logging for the symlink downloader to troubleshoot.
- Added some logging when deleting torrents and the symlinker overrides the finish action.

## [2.0.53] - 2024-01-05
### Added
- Add setting to set the download path on the aria2 instance.

## [2.0.52] - 2024-01-05
### Added
- Add BASE_PATH environment variable for the base path setting.
- Expose the Post Torrent Download Action setting on the Provider settings.

## [2.0.51] - 2024-01-05
### Added
- Added setting to store magnets and torrents to a directory after adding.
- Added bulk settings change on the index pages.
### Changed
- Swapped the internal downloader back to the one that was before, this one is giving too many headaches.
- Prevent deleting torrents from the debrid provider when the symlink downloader is used.
- Fixed %F parameter on the external program.
- Run the external program before the deletion process is ran.
- Remove the 100 char limit on inputs.

## [2.0.50] - 2023-11-25
### Changed
- Fixed Docker Builds for arm64.

## [2.0.49] - 2023-11-24
### Changed
- Fixed memory issue in internal downloader.
- Changed unpack process to handle cancels.

## [2.0.48] - 2023-11-15
### Changed
- Reverted dockerfile again as the packagemanager still doesn't have .net 8.

## [2.0.47] - 2023-11-15
### Changed
- Changed docker to use the package manager again.

## [2.0.46] - 2023-11-15
### Changed
- Fix in internal downloader.

## [2.0.45] - 2023-11-15
### Changed
- Optimizations to the internal downloader.

## [2.0.44] - 2023-11-15
### Changed
- Revert broken upgrade.

## [2.0.43] - 2023-11-15
### Changed
- Improvements to the internal downloader.

## [2.0.42] - 2023-11-14
### Changed
- Fixed docker build as .NET8 isn't published yet.

## [2.0.41] - 2023-11-14
### Changed
- Upgraded to .NET8 to see if downloader perf improves.

## [2.0.40] - 2023-10-03
### Changed
- Symlink downloader fixes.

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
