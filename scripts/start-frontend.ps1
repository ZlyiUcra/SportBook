<#
  Starts the frontend dev server. Assumes scripts/setup.ps1 has already been run at least once
  (yarn install done).
#>

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# See scripts/setup.ps1 for why this fallback exists.
if (-not (Get-Command yarn -ErrorAction SilentlyContinue)) {
    $yarnBin = Join-Path $env:USERPROFILE '.yarn\bin'
    if (Test-Path (Join-Path $yarnBin 'yarn.cmd')) {
        $env:PATH = "$yarnBin;$env:PATH"
    }
}

Set-Location (Join-Path $repoRoot 'frontend')
yarn dev
