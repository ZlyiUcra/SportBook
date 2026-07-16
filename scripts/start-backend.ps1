<#
  Starts the backend API in Development mode. Assumes scripts/setup.ps1 has already been run at
  least once (SQL Server up, secrets configured, database migrated).
#>

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = 'Development'

Set-Location (Join-Path $repoRoot 'backend\src\SportBook.Api')
dotnet run --no-launch-profile --urls "http://localhost:5217"
