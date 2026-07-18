<#
  One-time dataset conversion (research.md "Seeding mechanism"): turns a raw GeoNames dump for a
  single country into the committed backend/src/SportBook.Infrastructure/Data/cities.csv used by
  the CreateAndSeedCities migration. Not run automatically - re-run only when the seed dataset
  needs a refresh (new country, updated population figures).

  Expects three raw GeoNames files (CC BY 4.0, https://www.geonames.org/) already downloaded and
  extracted into -GeonamesDir:
    UA.txt                  - https://download.geonames.org/export/dump/UA.zip (main dump)
    alternateNamesUA.txt    - https://download.geonames.org/export/dump/alternatenames/UA.zip,
                               extracted file renamed from UA.txt to avoid colliding with the one
                               above
    admin1CodesASCII.txt    - https://download.geonames.org/export/dump/admin1CodesASCII.txt

  These raw files are not committed (large, external, refreshed independently of app code) -
  only this script's csv output is.
#>

param(
    [string]$GeonamesDir = (Join-Path (Split-Path -Parent $PSScriptRoot) '.geonames-src'),
    [int]$PopulationThreshold = 500,
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'backend/src/SportBook.Infrastructure/Data/cities.csv')
)

$ErrorActionPreference = 'Stop'

$mainPath = Join-Path $GeonamesDir 'UA.txt'
$altPath = Join-Path $GeonamesDir 'alternateNamesUA.txt'
$admin1Path = Join-Path $GeonamesDir 'admin1CodesASCII.txt'

foreach ($p in @($mainPath, $altPath, $admin1Path)) {
    if (-not (Test-Path $p)) {
        throw "Required input file not found: $p (see this script's header comment for where to download it)"
    }
}

Write-Host "==> Reading admin1 region names from $admin1Path" -ForegroundColor Cyan
$admin1 = @{}
foreach ($line in Get-Content -Path $admin1Path -Encoding UTF8) {
    if (-not $line) { continue }
    $f = $line -split "`t"
    if ($f.Length -lt 4) { continue }
    $admin1[$f[0]] = [pscustomobject]@{ NameEn = $f[1]; GeonameId = $f[3] }
}

Write-Host "==> Reading uk/pt alternate names from $altPath" -ForegroundColor Cyan
# geonameid -> @{ uk = name; pt = name }; isPreferredName=1 rows win over first-seen.
$altNames = @{}
foreach ($line in Get-Content -Path $altPath -Encoding UTF8) {
    if (-not $line) { continue }
    $f = $line -split "`t"
    if ($f.Length -lt 4) { continue }
    $lang = $f[2]
    if ($lang -ne 'uk' -and $lang -ne 'pt') { continue }
    $gid = $f[1]
    $name = $f[3]
    $isPreferred = ($f.Length -gt 4) -and ($f[4] -eq '1')
    if (-not $altNames.ContainsKey($gid)) { $altNames[$gid] = @{} }
    $bucket = $altNames[$gid]
    if ($isPreferred -or -not $bucket.ContainsKey($lang)) {
        $bucket[$lang] = $name
    }
}

function Get-LocalName {
    param([string]$GeonameId, [string]$Lang, [string]$Fallback)
    if ($GeonameId -and $altNames.ContainsKey($GeonameId) -and $altNames[$GeonameId].ContainsKey($Lang)) {
        return $altNames[$GeonameId][$Lang]
    }
    return $Fallback
}

Write-Host "==> Reading populated places from $mainPath" -ForegroundColor Cyan
$populatedPlaces = New-Object System.Collections.Generic.List[object]
foreach ($line in Get-Content -Path $mainPath -Encoding UTF8) {
    if (-not $line) { continue }
    $f = $line -split "`t"
    if ($f.Length -lt 15) { continue }
    if ($f[6] -ne 'P') { continue }
    $population = 0
    [void][int]::TryParse($f[14], [ref]$population)
    $populatedPlaces.Add([pscustomobject]@{
        Id          = $f[0]
        NameEn      = $f[1]
        CountryCode = $f[8]
        Admin1Code  = "$($f[8]).$($f[10])"
        Latitude    = $f[4]
        Longitude   = $f[5]
        Population  = $population
    })
}

Write-Host "Populated places (feature class P): $($populatedPlaces.Count) total" -ForegroundColor Yellow
foreach ($t in 500, 1000, 5000) {
    $count = ($populatedPlaces | Where-Object { $_.Population -ge $t }).Count
    Write-Host ("  population >= {0,5}: {1}" -f $t, $count) -ForegroundColor Yellow
}

$selected = $populatedPlaces | Where-Object { $_.Population -ge $PopulationThreshold }
Write-Host "==> Using threshold $PopulationThreshold -> $($selected.Count) cities selected" -ForegroundColor Cyan

$unresolvedRegions = 0
$rows = foreach ($c in $selected) {
    $region = $admin1[$c.Admin1Code]
    if (-not $region) { $unresolvedRegions++ }
    $regionEn = if ($region) { $region.NameEn } else { '' }
    $regionGid = if ($region) { $region.GeonameId } else { $null }

    [pscustomobject]@{
        Id          = $c.Id
        NameEn      = $c.NameEn
        NameUk      = Get-LocalName -GeonameId $c.Id -Lang 'uk' -Fallback $c.NameEn
        NamePt      = Get-LocalName -GeonameId $c.Id -Lang 'pt' -Fallback $c.NameEn
        CountryCode = $c.CountryCode
        RegionEn    = $regionEn
        RegionUk    = Get-LocalName -GeonameId $regionGid -Lang 'uk' -Fallback $regionEn
        RegionPt    = Get-LocalName -GeonameId $regionGid -Lang 'pt' -Fallback $regionEn
        Latitude    = $c.Latitude
        Longitude   = $c.Longitude
        Population  = $c.Population
    }
}

if ($unresolvedRegions -gt 0) {
    Write-Warning "$unresolvedRegions cities had no matching admin1 region name (RegionEn left empty)."
}

$outDir = Split-Path -Parent $OutputPath
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$rows | Sort-Object { [int]$_.Id } | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8
Write-Host "==> Wrote $($rows.Count) cities to $OutputPath" -ForegroundColor Green
