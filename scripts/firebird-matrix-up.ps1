Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

docker compose -f docker-compose.firebird.yml up -d

Write-Host "Firebird matrix starting. Waiting 20 seconds before tests..."
Start-Sleep -Seconds 20
docker compose -f docker-compose.firebird.yml ps
