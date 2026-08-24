Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:FIREBIRDADMIN_FB25_HOST = "localhost"
$env:FIREBIRDADMIN_FB25_PORT = "3025"
$env:FIREBIRDADMIN_FB25_DATABASE = "/firebird/data/firebirdadmin.fdb"
$env:FIREBIRDADMIN_FB25_USER = "SYSDBA"
$env:FIREBIRDADMIN_FB25_PASSWORD = "masterkey"

$env:FIREBIRDADMIN_FB30_HOST = "localhost"
$env:FIREBIRDADMIN_FB30_PORT = "3030"
$env:FIREBIRDADMIN_FB30_DATABASE = "/var/lib/firebird/data/firebirdadmin.fdb"
$env:FIREBIRDADMIN_FB30_USER = "SYSDBA"
$env:FIREBIRDADMIN_FB30_PASSWORD = "masterkey"

$env:FIREBIRDADMIN_FB40_HOST = "localhost"
$env:FIREBIRDADMIN_FB40_PORT = "3040"
$env:FIREBIRDADMIN_FB40_DATABASE = "/var/lib/firebird/data/firebirdadmin.fdb"
$env:FIREBIRDADMIN_FB40_USER = "SYSDBA"
$env:FIREBIRDADMIN_FB40_PASSWORD = "masterkey"

$env:FIREBIRDADMIN_FB50_HOST = "localhost"
$env:FIREBIRDADMIN_FB50_PORT = "3055"
$env:FIREBIRDADMIN_FB50_DATABASE = "/var/lib/firebird/data/firebirdadmin.fdb"
$env:FIREBIRDADMIN_FB50_USER = "SYSDBA"
$env:FIREBIRDADMIN_FB50_PASSWORD = "masterkey"

dotnet test tests/FirebirdAdmin.IntegrationTests/FirebirdAdmin.IntegrationTests.csproj
