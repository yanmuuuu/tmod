# ============================================================================
#  WastelandSoul - CLIENT-mode load test
#
#  Why this exists (a real accident, see 开发说明.md 批次 20):
#    ws_pipeline.ps1's isolated load test runs `-server`. Under Main.dedServ the
#    client-only code paths never execute, so a crash like
#        System.Threading.ThreadStateException: most FNA3D audio/graphics
#        functions must be called on the main thread -> Disabling Mod: WastelandSoul
#    passes the server test and still kills the game for the player.
#    This script boots the real CLIENT (no -server) against an isolated save dir,
#    waits for "Mod Load Completed", kills it, and greps the log for trouble.
#
#  Usage:
#    powershell -NoProfile -ExecutionPolicy Bypass -File E:\开发\tools\client_loadtest.ps1
#  Options:
#    -WaitForGame <seconds>  wait until Mods\*.tmod is writable before starting
#    -TimeoutSeconds <n>     how long to wait for the client to finish loading
#
#  English-only on purpose (PowerShell 5.1 + UTF-8 without BOM issue); must keep a BOM.
# ============================================================================
param(
    [string]$TmlDir = 'e:\steam\steamapps\common\tModLoader',
    [string]$TestDir = 'E:\开发\.tml-loadtest-cn',
    [string]$BuildDir = 'E:\开发\.tml-build',
    [string]$RealMods = "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\Mods",
    [int]$TimeoutSeconds = 240,
    [int]$WaitForGame = 0
)

$ErrorActionPreference = 'Continue'
$logDir = Join-Path $TmlDir 'tModLoader-Logs'
$clientLog = Join-Path $logDir 'client.log'

function Wait-Unlocked($path, $seconds) {
    if ($seconds -le 0) { return $true }

    $deadline = (Get-Date).AddSeconds($seconds)
    $said = $false

    while ((Get-Date) -lt $deadline) {
        try {
            $fs = [System.IO.File]::Open($path, 'Open', 'ReadWrite', 'None')
            $fs.Close()
            return $true
        } catch {
            if (-not $said) {
                Write-Host ("  [WAIT] game still holds {0} - waiting up to {1}s" -f (Split-Path $path -Leaf), $seconds)
                $said = $true
            }
            Start-Sleep -Seconds 5
        }
    }

    return $false
}

Write-Host ''
Write-Host '=== client-mode load test ==='

# 0) wait for the running game to release the mod files
foreach ($name in @('WastelandSoul.tmod', 'WastelandSoulCN.tmod')) {
    $target = Join-Path $RealMods $name

    if (Test-Path $target) {
        [void](Wait-Unlocked $target $WaitForGame)
    }
}

# 1) copy the freshly built mods into the isolated test dir
$testMods = Join-Path $TestDir 'Mods'
New-Item -ItemType Directory -Force $testMods | Out-Null

foreach ($name in @('WastelandSoul.tmod', 'WastelandSoulCN.tmod')) {
    $src = Join-Path $BuildDir "Mods\$name"

    if (Test-Path $src) {
        Copy-Item $src $testMods -Force
    } else {
        Write-Host ("  [FAIL] missing build output {0}" -f $src)
        exit 2
    }
}

# 2) keep the player's previous client.log (a new launch truncates it)
New-Item -ItemType Directory -Force 'E:\开发\.tmp-merge\log-backup' | Out-Null

if (Test-Path $clientLog) {
    Copy-Item $clientLog 'E:\开发\.tmp-merge\log-backup\client.log.before' -Force
}

# 3) boot the CLIENT (no -server) against the isolated save dir
$proc = Start-Process -FilePath 'dotnet' `
    -ArgumentList @("$TmlDir\tModLoader.dll", '-tmlsavedirectory', $TestDir) `
    -WorkingDirectory $TmlDir -PassThru -NoNewWindow

Write-Host ("  [..] client pid {0}, waiting for 'Mod Load Completed' (max {1}s)" -f $proc.Id, $TimeoutSeconds)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$loaded = $false

while ((Get-Date) -lt $deadline -and -not $proc.HasExited) {
    Start-Sleep -Seconds 3

    if ((Test-Path $clientLog) -and (Select-String -Path $clientLog -Pattern 'Mod Load Completed' -Quiet -ErrorAction SilentlyContinue)) {
        $loaded = $true
        break
    }
}

if (-not $proc.HasExited) {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 2

# 4) verdict
$content = @()

if (Test-Path $clientLog) {
    $content = Get-Content $clientLog -Encoding UTF8 -ErrorAction SilentlyContinue
}

$patterns = 'Disabling Mod|ThreadStateException|must be called on the main thread|error occurred while loading|automatically disabled|doesn''t match the filename|Exception'
$bad = @($content | Where-Object { $_ -match $patterns })
$ours = @($content | Where-Object { $_ -match 'WastelandSoul' })

Write-Host ''
Write-Host ("  Mod Load Completed: {0}" -f $loaded)
Write-Host ("  提到 WastelandSoul 的行: {0}" -f $ours.Count)

if ($bad.Count -eq 0 -and $loaded) {
    Write-Host '  [OK] client loaded the mods with no errors'
    exit 0
}

Write-Host '  [FAIL] client load test'
$bad | Select-Object -First 12 | ForEach-Object { Write-Host ('     ' + $_.Trim()) }
exit 1
