param([string]$Configuration = "Release", [string]$RevitApiDir = "C:\Program Files\Autodesk\Revit 2025")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$plugin = Join-Path $root "src\WallFinishGlobal\WallFinishGlobal.csproj"
$installer = Join-Path $root "installer\WallFinishInstaller\WallFinishInstaller.csproj"
$pluginOut = Join-Path $root "src\WallFinishGlobal\bin\$Configuration"
$payload = Join-Path $root "installer\WallFinishInstaller\payload"
$publish = Join-Path $root "artifacts\installer"
dotnet build $plugin -c $Configuration /p:RevitApiDir="$RevitApiDir"
if ($LASTEXITCODE -ne 0) { throw "Revit add-in build failed." }
New-Item -ItemType Directory -Force $payload | Out-Null
New-Item -ItemType Directory -Force $publish | Out-Null
Copy-Item (Join-Path $pluginOut "WallFinishGlobal.dll") (Join-Path $payload "WallFinishGlobal.dll") -Force
Copy-Item (Join-Path $root "src\WallFinishGlobal\Resources\finish-rules.json") (Join-Path $payload "finish-rules.json") -Force
dotnet publish $installer -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=true -o $publish
if ($LASTEXITCODE -ne 0) { throw "Windows installer publish failed." }
Write-Host "Installer EXE: $(Join-Path $publish 'WallFinishInstaller.exe')"
