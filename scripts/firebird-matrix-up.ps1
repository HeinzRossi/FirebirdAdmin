Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

docker compose -f docker-compose.firebird.yml up -d

Write-Host "Firebird matrix starting. Give containers a few seconds before running scripts/firebird-matrix-test.ps1."
