<#
  Starts (or stops) both dev servers from one script.

  Start (default): runs setup.ps1 (idempotent), launches the backend and frontend each in their
  own PowerShell window, waits for each to actually respond, and then HIDES the window for any
  service that came up successfully - the process keeps running in the background, only the
  window disappears. A window is left visible only if its service failed to come up in time, so
  you can read the error.

  Stop: powershell -File scripts/start.ps1 -Stop
  Finds whatever is listening on the backend/frontend ports and stops it - works regardless of
  whether the window is hidden or visible, and even if it wasn't started by this script.

  Run from anywhere: powershell -File scripts/start.ps1
#>

param(
    [switch]$Stop
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot
$backendUrl = 'http://localhost:5217/api/venues'
$frontendUrl = 'http://localhost:5173/'
$backendPort = 5217
$frontendPort = 5173

function Stop-ByPort {
    param([int]$Port, [string]$Label)
    $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $conn) {
        Write-Host "$Label (port $Port) was not running." -ForegroundColor DarkGray
        return
    }

    # Walk up the parent chain first (e.g. the hidden -NoExit wrapper window, or yarn's cmd.exe
    # wrapper) so nothing is left running once the actual server process is gone - killing only
    # the port-owning process leaves an empty, hidden, still-running shell behind.
    $pidsToKill = [System.Collections.Generic.List[int]]::new()
    $currentPid = $conn.OwningProcess
    for ($i = 0; $i -lt 5 -and $currentPid -and $currentPid -ne 0; $i++) {
        $pidsToKill.Add($currentPid)
        $wmiProcess = Get-CimInstance Win32_Process -Filter "ProcessId=$currentPid" -ErrorAction SilentlyContinue
        if (-not $wmiProcess -or -not $wmiProcess.ParentProcessId) { break }
        $parentProcess = Get-Process -Id $wmiProcess.ParentProcessId -ErrorAction SilentlyContinue
        # dotnet.exe (dotnet run's own wrapper) and cmd.exe (yarn.cmd's interpreter) are both
        # intermediate hops worth walking through to reach the real -NoExit window on top.
        if (-not $parentProcess -or $parentProcess.ProcessName -notin @('powershell', 'pwsh', 'cmd', 'dotnet')) { break }
        $currentPid = $wmiProcess.ParentProcessId
    }

    foreach ($pidToKill in $pidsToKill) {
        Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Stopped $Label (port $Port, PID $($conn.OwningProcess); killed $($pidsToKill.Count) process(es) in its chain)." -ForegroundColor Yellow
}

if ($Stop) {
    Write-Host "==> Stopping SportBook..." -ForegroundColor Cyan
    Stop-ByPort -Port $backendPort -Label 'Backend'
    Stop-ByPort -Port $frontendPort -Label 'Frontend'
    return
}

# --- start path ---

Add-Type -Name Win32ShowWindow -Namespace SportBook -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
'@ -ErrorAction SilentlyContinue

function Wait-ForHttp {
    param([string]$Url, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop | Out-Null
            return $true
        } catch {
            # Any HTTP response at all (even an error status like 401) means the server is up.
            if ($_.Exception.Response) { return $true }
        }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Hide-ProcessWindow {
    param($Process)
    $Process.Refresh()
    $deadline = (Get-Date).AddSeconds(5)
    while ($Process.MainWindowHandle -eq 0 -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 200
        $Process.Refresh()
    }
    if ($Process.MainWindowHandle -ne 0) {
        [SportBook.Win32ShowWindow]::ShowWindow($Process.MainWindowHandle, 0) | Out-Null
    }
}

Write-Host "==> Running setup..." -ForegroundColor Cyan
& (Join-Path $scriptDir 'setup.ps1')

Write-Host "==> Launching backend..." -ForegroundColor Cyan
$backendProc = Start-Process powershell -ArgumentList '-NoExit', '-File', (Join-Path $scriptDir 'start-backend.ps1') -PassThru

Write-Host "==> Launching frontend..." -ForegroundColor Cyan
$frontendProc = Start-Process powershell -ArgumentList '-NoExit', '-File', (Join-Path $scriptDir 'start-frontend.ps1') -PassThru

Write-Host "==> Waiting for both to respond..." -ForegroundColor Cyan
$backendReady = Wait-ForHttp -Url $backendUrl -TimeoutSeconds 60
$frontendReady = Wait-ForHttp -Url $frontendUrl -TimeoutSeconds 30

Write-Host ""
if ($backendReady) {
    Hide-ProcessWindow -Process $backendProc
    Write-Host "Backend ready:  http://localhost:5217 (window hidden, still running)" -ForegroundColor Green
} else {
    Write-Host "Backend did not respond in time - its window was left open, check it for errors." -ForegroundColor Red
}

if ($frontendReady) {
    Hide-ProcessWindow -Process $frontendProc
    Write-Host "Frontend ready: http://localhost:5173 (window hidden, still running)" -ForegroundColor Green
} else {
    Write-Host "Frontend did not respond in time - its window was left open, check it for errors." -ForegroundColor Red
}

Write-Host ""
Write-Host "To stop both: powershell -File scripts/start.ps1 -Stop" -ForegroundColor Cyan
