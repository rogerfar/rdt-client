$currentDirectory = $PSScriptRoot

Write-Host "Starting update script in $currentDirectory"

If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{   
    $arguments = "& '" + $myinvocation.mycommand.definition + "'"
    Start-Process powershell -Verb runAs -ArgumentList $arguments
    Break
}

Write-Host "Stopping ReadDebridClient..."

Stop-Service RealDebridClient

Write-Host "Stopped ReadDebridClient"

$releasesUri = "https://api.github.com/repos/rogerfar/rdt-client/releases/latest"
$downloadUri = ((Invoke-RestMethod -Method GET -Uri $releasesUri).assets | Where-Object name -like "*.zip").browser_download_url

Write-Host "Downloading $downloadUri"

$pathZip = Join-Path -Path $([System.IO.Path]::GetTempPath()) -ChildPath $(Split-Path -Path $downloadUri -Leaf)

Invoke-WebRequest -Uri $downloadUri -Out $pathZip

$tempExtract = Join-Path -Path $([System.IO.Path]::GetTempPath()) -ChildPath $((New-Guid).Guid)

Write-Host "Extracting to $tempExtract"

Expand-Archive -Path $pathZip -DestinationPath $tempExtract -Force

Write-Host "Backing up appsettings.json"

Copy-Item -Path "$currentDirectory\appsettings.json" -Destination $tempExtract -Force 

Write-Host "Moving new files"

Move-Item -Path "$tempExtract\*" -Destination $currentDirectory -Force

Write-Host "Removing temp files"

Remove-Item -Path $tempExtract -Force -Recurse -ErrorAction SilentlyContinue

Remove-Item $pathZip -Force

Write-Host "Starting ReadDebridClient..."

Start-Service RealDebridClient

Write-Host "Started ReadDebridClient"