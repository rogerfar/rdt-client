If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{   
    $arguments = "& '" + $myinvocation.mycommand.definition + "'"
    Start-Process powershell -Verb runAs -ArgumentList $arguments
    Break
}

Stop-Service RealDebridClient

$releasesUri = "https://api.github.com/repos/rogerfar/rdt-client/releases/latest"
$downloadUri = ((Invoke-RestMethod -Method GET -Uri $releasesUri).assets | Where-Object name -like "*.zip").browser_download_url

$pathZip = Join-Path -Path $([System.IO.Path]::GetTempPath()) -ChildPath $(Split-Path -Path $downloadUri -Leaf)

Invoke-WebRequest -Uri $downloadUri -Out $pathZip

$tempExtract = Join-Path -Path $([System.IO.Path]::GetTempPath()) -ChildPath $((New-Guid).Guid)

Expand-Archive -Path $pathZip -DestinationPath $tempExtract -Force
Copy-Item -Path ".\appsettings.json" -Destination $tempExtract -Force 
Move-Item -Path "$tempExtract\*" -Destination $pathExtract -Force
Remove-Item -Path $tempExtract -Force -Recurse -ErrorAction SilentlyContinue

Remove-Item $pathZip -Force

Start-Service RealDebridClient