#!/usr/bin/env pwsh
<#
        .SYNOPSIS
        Helper script to execute docker buildx

        .DESCRIPTION
        Simplifies executing docker buildx for multi-architecture for the current Dockerfile 

        .PARAMETER Version
        Specifies the version number to take this image with (defaults to 'latest' only)

        .PARAMETER DockerAccount
        The docker account to use to push the image

        .PARAMETER Platforms
        The platforms to target for the image (defaults to linux/arm/v7,linux/arm64/v8,linux/amd64)

        .INPUTS
        None. You cannot pipe objects to to this script.

        .OUTPUTS
        None

        .EXAMPLE
        PS> /docker-build.ps1        

        .EXAMPLE
        PS> ./docker-build.ps1 -Version v1.7.4
        
        .EXAMPLE
        PS> ./docker-build.ps1 -Platforms "linux/arms64/v8"
        
    #>
param(
    [string]$Version = "",
    [string]$DockerAccount = "rogerfar",
    [string]$Platforms = "linux/arm64/v8,linux/amd64",
    [string]$Dockerfile = "Dockerfile",
    [switch]$SkipPush,
    [switch]$SkipCache,
    [switch]$OutputToDocker,
    [string]$BuildProgress="auto",
    [switch]$AutoRun
)

# Set Defaults
$defaultPort = 6500
$appName = "rdtclient"
$imageName = "$($DockerAccount)/$($appName)"

# Load version details (if needed)
if ([string]::IsNullOrEmpty($Version)) { 
	$Version = (Get-Content "package.json" | ConvertFrom-Json).version
}

# Define the default arguments for docker build and run
$dockerCommandArgsList = @()

$baseDockerBuildArgs = @( "buildx", "build", "--network=default", "--progress=$BuildProgress", "--file", $Dockerfile, "--build-arg", "VERSION=$($Version)", "." )
$baseDockerRunArgs = @( "run", "--cap-add=NET_ADMIN", "-d", "--log-driver", "json-file", "--log-opt", "max-size=10m" )

# Add any conditional argumes
if (-Not $SkipPush.IsPresent)   { $baseDockerBuildArgs += @("--push") }
if ($SkipCache.IsPresent)       { $baseDockerBuildArgs += @("--no-cache") }
if ($OutputToDocker.IsPresent)  { $baseDockerBuildArgs += @("--load")  }

# If we have multiple platforms AND we want to export them to locally to docker so we can run them
# we need to build and tag each arch individually
$lstPLatforms = $Platforms -split "," 
if ($OutputToDocker.IsPresent -and $lstPLatforms.Count -gt 0) {
    $extPort=$defaultPort
    foreach ($p in $lstPLatforms) {
        $extPort++
        $pn = $p -replace "/", "-"

        $dbArgs = $baseDockerBuildArgs
        $dbArgs += @("--tag", "${imageName}:${Version}-${pn}")
        $dbArgs += @("--platform", $p)

        $drArgs = $baseDockerRunArgs
        $drArgs += @( "-v", "${PWD}/data/${pn}/db:/data/db" )
        $drArgs += @( "-v", "${PWD}/data/${pn}/downloads:/data/downloads" )
        $drArgs += @( "--platform", $p )
        $drArgs += @( "-p", "${extPort}:${defaultPort}")
        $drArgs += @( "--name", "${appName}-${pn}" )
        $drArgs += @( "${imageName}:${Version}-${pn}")

        $o = @{
            ImageName = "${imageName}:${Version}-${pn}"
            AppName = "${appName}-${pn}"
            Platform = $p
            CmdArgs = $dbArgs
            RunArgs = $drArgs
        }

        $dockerCommandArgsList += @(, $o)
    }
} else {
    # Looks like we don't need to load them locally so lets build everything at once as a sinlge mutli-arch image
    $dbArgs = $baseDockerBuildArgs
    $dbArgs += @("--tag", "${imageName}:${Version}")
    $dbArgs += @("--platform", $Platforms)

    $drArgs = $baseDockerRunArgs
    $drArgs += @( "-v", "${PWD}/data/db:/data/db" )
    $drArgs += @( "-v", "${PWD}/data/downloads:/data/downloads" )
    $drArgs += @( "-p ${defaultPort}:${defaultPort}")
    $drArgs += @( "--name", "${appName}" )
    $drArgs += @( "${imageName}:${Version}")

    $o = @{
        ImageName = "${imageName}:${Version}"
        AppName = "${appName}"
        Platform = $Platforms
        CmdArgs = $ddbArgs
        RunArgs = $drArgs
    }

    $dockerCommandArgsList += @(, $o)
}

# Lets trigger the builds
foreach ($c in $dockerCommandArgsList) {
    Write-Host "Generating docker image $imageName for $($c.Platform)" -ForegroundColor Green
    Write-Host "Args: $($c.CmdArgs)" -ForegroundColor Yellow
    &docker $($c.CmdArgs)

    if ($AutoRun.IsPresent) {
        Write-Host "Running docker image $imageName for $($c.Platform)" -ForegroundColor Green
        Write-Host "Args: $($c.RunArgs)" -ForegroundColor Yellow
        &docker $($c.RunArgs)

        Write-Host "Sleeping for 30s (allowing containers to start)" -ForegroundColor Green
        Start-Sleep -Seconds 30

        Write-Host "Checking on status of containers" -ForegroundColor Green
        &docker container ls --all --filter "name=$($c.AppName)" --format '{{json .}}' | jq --slurp | ConvertFrom-Json | % {
            if ($d.Status -contains "*unhealthy*" ) {
                Write-Host "Image ${c.ImageName} is not starting correctly" -ForegroundColor Red
            } else {
                Write-Host "Image ${c.ImageName} is starting correctly" -ForegroundColor Green
            }
        }

    }
}

# # Do we want to automatically start them after they are build?
# if ($AutoRun.IsPresent) {
#     foreach ($c in $dockerCommandArgsList) {
#         Write-Host "Running docker image $imageName for $($c.Platform)" -ForegroundColor Green
#         Write-Host "Args: $($c.RunArgs)" -ForegroundColor Yellow
#         docker $($c.RunArgs)
#     }

#     Write-Host "Sleeping for 30s (allowing containers to start)" -ForegroundColor Green
#     Start-Sleep -Seconds 30

#     Write-Host "Checking on status of containers" -ForegroundColor Green
#     docker container ls --all --filter "name=$appName" --format '{{json .}}' | jq --slurp | ConvertFrom-Json | % {
#         if ($d.Status -contains "*unhealthy*" ) {
#             Write-Host "Image ${c.ImageName} is not starting correctly" -ForegroundColor Red
#         } else {
#             Write-Host "Image ${c.ImageName} is starting correctly" -ForegroundColor Green
#         }
#     }
# }
