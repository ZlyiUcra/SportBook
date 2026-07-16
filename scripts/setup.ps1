<#
  One-time, idempotent local environment setup: starts the SQL Server container, creates the
  least-privilege application login, seeds default dev secrets (only if not already set - this
  never overwrites secrets you have already customized), applies EF Core migrations, and
  installs frontend dependencies. Safe to re-run any time.

  The sample values below (SQL login password, JWT signing key) are for solo local development
  only. Change them - edit the two variables just below - if this machine is shared with anyone
  else, or if any port here is ever exposed beyond localhost.
#>

$ErrorActionPreference = 'Stop'

$sqlAppLoginPassword = 'Sb_App_Login_Dev9!'
$sampleJwtKey = 'SportBook-Local-Dev-Signing-Key-Change-Me-0123456789'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Some yarn installs (the classic .msi/.cmd installer) only add themselves to a POSIX shell's
# PATH (e.g. Git Bash), not the Windows user/system PATH that PowerShell inherits - fall back to
# the known install location for this session only, without touching the persistent PATH.
if (-not (Get-Command yarn -ErrorAction SilentlyContinue)) {
    $yarnBin = Join-Path $env:USERPROFILE '.yarn\bin'
    if (Test-Path (Join-Path $yarnBin 'yarn.cmd')) {
        $env:PATH = "$yarnBin;$env:PATH"
    }
}

Write-Host "==> Starting SQL Server container..." -ForegroundColor Cyan
docker compose up -d

Write-Host "==> Waiting for SQL Server to become healthy..." -ForegroundColor Cyan
$status = $null
$deadline = (Get-Date).AddMinutes(3)
do {
    try { $status = docker inspect --format '{{.State.Health.Status}}' sportbook-mssql 2>$null } catch { $status = $null }
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 5
} while ((Get-Date) -lt $deadline)

if ($status -ne 'healthy') {
    Write-Error "SQL Server container did not become healthy in time. Check 'docker compose logs mssql'."
}
Write-Host "    SQL Server is healthy." -ForegroundColor Green

Write-Host "==> Ensuring the sportbook_app SQL login exists..." -ForegroundColor Cyan
$sql = "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'sportbook_app') BEGIN CREATE LOGIN sportbook_app WITH PASSWORD = '$sqlAppLoginPassword', CHECK_POLICY = ON; END; ALTER SERVER ROLE dbcreator ADD MEMBER sportbook_app;"
docker exec sportbook-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SportBook_Dev_Passw0rd" -C -b -Q $sql

$apiProject = Join-Path $repoRoot 'backend\src\SportBook.Api'
Push-Location $apiProject
try {
    Write-Host "==> Configuring backend secrets (only fills in values that aren't already set)..." -ForegroundColor Cyan
    dotnet user-secrets init | Out-Null
    $existingSecrets = (dotnet user-secrets list 2>$null) -join "`n"

    if ($existingSecrets -notmatch 'ConnectionStrings:DefaultConnection') {
        $connectionString = "Server=127.0.0.1,14330;Database=SportBookDb;User Id=sportbook_app;Password=$sqlAppLoginPassword;TrustServerCertificate=True;Encrypt=True"
        dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString | Out-Null
        Write-Host "    Set ConnectionStrings:DefaultConnection (sample value - fine for solo local dev)." -ForegroundColor Yellow
    } else {
        Write-Host "    ConnectionStrings:DefaultConnection already set - left unchanged." -ForegroundColor DarkGray
    }

    if ($existingSecrets -notmatch 'Jwt:Key') {
        dotnet user-secrets set "Jwt:Key" $sampleJwtKey | Out-Null
        Write-Host "    Set Jwt:Key (sample value - change it if this stops being solo local dev)." -ForegroundColor Yellow
    } else {
        Write-Host "    Jwt:Key already set - left unchanged." -ForegroundColor DarkGray
    }
} finally {
    Pop-Location
}

Write-Host "==> Restoring backend and applying migrations..." -ForegroundColor Cyan
dotnet restore (Join-Path $repoRoot 'SportBook.sln')
dotnet ef database update --project (Join-Path $repoRoot 'backend\src\SportBook.Infrastructure') --startup-project $apiProject

Write-Host "==> Installing frontend dependencies..." -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot 'frontend')
try { yarn install } finally { Pop-Location }

Write-Host "==> Setup complete." -ForegroundColor Green
