param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishRoot = Join-Path $repoRoot "artifacts\publish\FirebirdAdmin"
$releaseRoot = Join-Path $repoRoot "artifacts\releases"
$zipPath = Join-Path $releaseRoot "FirebirdAdmin-v$Version-$Runtime.zip"

Set-Location $repoRoot

dotnet restore FirebirdAdmin.sln
dotnet build FirebirdAdmin.sln `
    --configuration $Configuration `
    --no-restore `
    -m:1 `
    -p:BuildInParallel=false `
    -p:UseSharedCompilation=false `
    -p:NodeReuse=false

dotnet test FirebirdAdmin.sln `
    --configuration $Configuration `
    --no-build `
    -m:1 `
    -p:BuildInParallel=false `
    -p:UseSharedCompilation=false `
    -p:NodeReuse=false

if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot, $releaseRoot | Out-Null

dotnet publish src\FirebirdAdmin.Bootstrapper\FirebirdAdmin.Bootstrapper.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishRoot `
    -p:PublishSingleFile=false `
    -p:BuildInParallel=false `
    -p:UseSharedCompilation=false `
    -p:NodeReuse=false

dotnet build-server shutdown | Out-Host

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishRoot "*") -DestinationPath $zipPath -Force

Write-Host "Release package: $zipPath"
