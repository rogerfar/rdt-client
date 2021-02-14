cd client
npm install
ng build --prod --output-path=..\server\RdtClient.Web\wwwroot

cd ..
cd server
dotnet build -c Release
dotnet publish -c Release -o ..\out

cd ..
cd out

Add-Type -Assembly System.IO.Compression.FileSystem
$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
$location = Get-Location
[System.IO.Compression.ZipFile]::CreateFromDirectory($location, "$location/../RealDebridClient.zip", $compressionLevel, $false)

cd ..

Remove-Item -Path out -Recurse -Force

gh-release --assets RealDebridClient.zip

Remove-Item RealDebridClient.zip