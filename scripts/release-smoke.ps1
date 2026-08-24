param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishRoot = Join-Path $repoRoot "artifacts\publish\FirebirdAdmin"
$exePath = Join-Path $publishRoot "FirebirdAdmin.Bootstrapper.exe"
$logRoot = Join-Path $env:LOCALAPPDATA "FirebirdAdmin\Logs"

if (-not (Test-Path $exePath)) {
    throw "Published executable not found: $exePath. Run scripts\release-publish.ps1 first."
}

$before = @()
if (Test-Path $logRoot) {
    $before = Get-ChildItem -Path $logRoot -Filter "firebird-admin-*.log" -File -ErrorAction SilentlyContinue
}

$process = Start-Process -FilePath $exePath -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 8

if ($process.HasExited) {
    throw "Smoke failed: app exited early with code $($process.ExitCode)."
}

Stop-Process -Id $process.Id -Force
$process.WaitForExit()
Start-Sleep -Seconds 1

$after = @()
if (Test-Path $logRoot) {
    $after = Get-ChildItem -Path $logRoot -Filter "firebird-admin-*.log" -File -ErrorAction SilentlyContinue
}

if ($after.Count -lt $before.Count -and $after.Count -eq 0) {
    throw "Smoke failed: log directory was not created at $logRoot."
}

Write-Host "Smoke OK: $exePath"
