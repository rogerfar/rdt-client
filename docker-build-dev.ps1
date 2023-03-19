#!/usr/bin/env pwsh

param(
    [string]$TempPath="c:/Temp/RtdClient",
    [string]$Dockerfile="Dockerfile",
    [switch]$AutoAttach,
    [switch]$SkipCache,
    [string]$BuildProgress="auto"
)

[string] $downloadPath = Join-Path -Path $TempPath -ChildPath "downloads"
[string] $dbPath = Join-Path -Path $TempPath -ChildPath "db"

Write-Host "Stopping Container (if already running)"
docker stop rdtclientdev

Write-Host "removing Container (if exists)"
docker rm rdtclientdev

Write-Host "Building Container"
$dockerArgs = @( "build", "--force-rm", "--tag", "rdtclientdev", "--progress=$BuildProgress", "--file", $Dockerfile, "." )
if ($SkipCache.IsPresent) { $dockerArgs += @("--no-cache" ) }

& docker $dockerArgs

Write-Host "Starting Container"
& docker run --cap-add=NET_ADMIN -d -v ${$downloadPath}:/data/downloads -v ${$dbPath}:/data/db --log-driver json-file --log-opt max-size=10m -p 6500:6500 --name rdtclientdev rdtclientdev

if ($AutoAttach.IsPresent) {
    docker exec -it rdtclientdev /bin/bash
}
