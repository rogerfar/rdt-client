$utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False

$version = (Get-Content "package.json" | ConvertFrom-Json).version

$csProj = "$pwd\server\RdtClient.Web\RdtClient.Web.csproj"
$navbar = "$pwd\client\src\app\navbar\navbar.component.html";

$newCsProj = (Get-Content $csProj) -replace '<Version>.*?<\/Version>', "<Version>$version</Version>" 
[System.IO.File]::WriteAllLines($csProj, $newCsProj, $utf8NoBomEncoding)

$newNavbar = (Get-Content $navbar) -replace 'Version .*?<', "Version $version<"
[System.IO.File]::WriteAllLines($navbar, $newNavbar, $utf8NoBomEncoding)

Write-Output "Commit and push now, press any key to continue"

[Console]::ReadKey()

cd client
npm install
ng build --configuration production --output-path=..\server\RdtClient.Web\wwwroot

cd ..
cd server
dotnet build -c Release
dotnet publish -c Release -o ..\out

cd ..
cd out

$location = Get-Location
[string]$Zip = "C:\Program Files\7-Zip\7z.exe"
[array]$arguments = "a", "-tzip", "-y", "$location/../RealDebridClient.zip", "."
& $Zip $arguments

cd ..

Remove-Item -Path out -Recurse -Force

gh-release --assets RealDebridClient.zip

Remove-Item RealDebridClient.zip