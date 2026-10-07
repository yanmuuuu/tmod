# ============================================================================
#  WastelandSoul - one-shot pipeline: build -> verify -> loadtest -> install
#
#  Usage (must run with -ExecutionPolicy Bypass):
#    powershell -NoProfile -ExecutionPolicy Bypass -File E:\开发\tools\ws_pipeline.ps1
#
#  Options:
#    -SkipBuild     reuse the existing .tmod in $SaveDir\Mods
#    -SkipLoadTest  skip the isolated headless load test
#    -SkipInstall   do not copy into the real game Mods folder
#
#  English-only on purpose: PowerShell 5.1 mis-parses UTF-8 scripts
#  without a BOM, so this file avoids non-ASCII characters entirely.
# ============================================================================
param(
    [string]$ModSource   = 'E:\开发\WastelandSoul',
    [string]$PatchSource = 'E:\开发\WastelandSoulCN',
    [string]$TmlDir      = 'e:\steam\steamapps\common\tModLoader',
    [string]$SaveDir     = 'E:\开发\.tml-build',
    [string]$TestDir     = 'E:\开发\.tml-loadtest-cn',
    [string]$Backup      = 'E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson',
    [string]$RealMods    = "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\Mods",
    [string]$Python      = 'C:\Users\p老师\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe',
    [int]$WaitForGame    = 0,
    [switch]$SkipBuild,
    [switch]$SkipLoadTest,
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Continue'
$failed = @()

# Python scripts print Chinese + check marks; without this they die with
# UnicodeEncodeError under the default GBK console encoding.
$env:PYTHONIOENCODING = 'utf-8'

function Step($name) { Write-Host ""; Write-Host "=== $name ===" }

# tModLoader keeps every .tmod in the Mods folder open while it runs, so installing
# while the game is up always fails. With -WaitForGame <seconds> we poll until the
# file becomes writable instead of failing (and instead of printing a fake [OK]).
function Wait-ForModUnlocked($path, $seconds) {
    if ($seconds -le 0) { return $true }

    $deadline = (Get-Date).AddSeconds($seconds)
    $announced = $false

    while ((Get-Date) -lt $deadline) {
        try {
            $stream = [System.IO.File]::Open($path, 'Open', 'ReadWrite', 'None')
            $stream.Close()
            return $true
        } catch {
            if (-not $announced) {
                Write-Host ("  [WAIT] {0} is locked by a running game - waiting up to {1}s" -f `
                    (Split-Path $path -Leaf), $seconds)
                $announced = $true
            }
            Start-Sleep -Seconds 5
        }
    }

    return $false
}

function RunBuild($source, $label) {
    Push-Location $TmlDir
    $out = & dotnet "$TmlDir\tModLoader.dll" -build $source -tmlsavedirectory $SaveDir 2>&1
    Pop-Location
    $text = ($out | Out-String)
    $ok = $text -match '0 errors and 0 warnings'
    if ($ok) {
        Write-Host ("  [OK]   {0}: 0 errors / 0 warnings" -f $label)
    } else {
        Write-Host ("  [FAIL] {0}" -f $label)
        $text -split "`n" | Where-Object { $_ -match 'error|Error' } | Select-Object -First 8 | ForEach-Object { Write-Host ("     " + $_.Trim()) }
        $script:failed += "build:$label"
    }
}

function RunChecker($script, $label, $arguments = @()) {
    if (-not (Test-Path $script)) { Write-Host ("  [SKIP] {0} (not found)" -f $label); return }
    $out = & $Python $script @arguments 2>&1
    $code = $LASTEXITCODE
    if ($code -eq 0) {
        Write-Host ("  [OK]   {0}" -f $label)
    } else {
        Write-Host ("  [FAIL] {0} (exit {1})" -f $label, $code)
        $out | Select-Object -Last 10 | ForEach-Object { Write-Host ("     " + $_) }
        $script:failed += "check:$label"
    }
}

# tModLoader renames localisation files it considers obsolete to "<name>.legacy".
# Those leftovers are poison: on the NEXT load it calls File.Move(good, good.legacy)
# with overwrite:false, the destination already exists, and the IOException makes
# tModLoader DISABLE the mod (and its dependencies) - while the console log only
# prints "When the file already exists, it cannot be created" + a server.log pointer,
# and a second load pass without the mod still reaches "Choose World".
# So: clear them before building and again right before the load test.
function Clear-LegacyLocalization($label) {
    $removed = 0
    foreach ($root in @($ModSource, $PatchSource)) {
        $dir = Join-Path $root 'Localization'
        if (-not (Test-Path $dir)) { continue }
        Get-ChildItem -Path $dir -Recurse -File -Filter '*.legacy' -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host ("  [CLEAN] removed {0}" -f $_.FullName)
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
            $removed++
        }
    }
    if ($removed -eq 0) { Write-Host ("  [OK]   no *.legacy leftovers ({0})" -f $label) }
}

# The load log prints mod DISPLAY names ("Adding Content: 废土魂穿 v0.1"), never the
# internal name, so take the display name from build.txt instead of hardcoding it.
function Get-ModDisplayName($source) {
    $build = Join-Path $source 'build.txt'
    if (-not (Test-Path $build)) { return $null }

    $line = Get-Content $build -Encoding UTF8 -ErrorAction SilentlyContinue |
        Where-Object { $_ -match '^\s*displayName\s*=' } | Select-Object -First 1

    if (-not $line) { return $null }
    return ($line -split '=', 2)[1].Trim()
}

# ------------------------------------------------- 1. localisation (sync + snapshot)
# ORDER MATTERS. Every build and every load test REWRITES the patch's zh-Hans
# file inside its source folder, and tModLoader only writes back a minimal key
# set (the rest becomes "// English" placeholder comments). So:
#
#   * the translation table in sync_cn_translation.py + the main mod's en-US file
#     are the ONLY source of truth, and
#   * the backup snapshot MUST be taken HERE - before the build and before the
#     load test - while the source file is still the freshly generated one.
#
# The old pipeline refreshed the backup inside step "install", i.e. AFTER the
# load test had already gutted the source file. That stored the damaged file as
# the backup, so "restore from backup" restored the damage: a self-copying bug.
# The snapshot is a copy of the file sync_cn_translation.py just wrote, and
# check_cn_backup.py proves it still matches the authoritative output.
Step '1. localisation: regenerate from the translation table + snapshot backup'
Clear-LegacyLocalization 'pre-build'
RunChecker "$PSScriptRoot\sync_cn_translation.py" 'sync_cn_translation (pre-build)'
RunChecker "$PSScriptRoot\check_cn_template.py"   'check_cn_template (English template rule)'
RunChecker "$PSScriptRoot\check_cn_backup.py"     'check_cn_backup (snapshot taken before any load test)'

# ---------------------------------------------------------------- 2. build
if (-not $SkipBuild) {
    Step '2. build'
    RunBuild $PatchSource 'WastelandSoulCN'
    RunBuild $ModSource   'WastelandSoul'
} else {
    Step '2. build (skipped)'
}

# ---------------------------------------------------------------- 3. checks
Step '3. static checks'
RunChecker "$PSScriptRoot\check_assets.py"       'check_assets'
RunChecker "$PSScriptRoot\check_localization.py" 'check_localization'
RunChecker "$PSScriptRoot\check_cn_parity.py"    'check_cn_parity'
RunChecker "$PSScriptRoot\check_cn_template.py"  'check_cn_template'
RunChecker "$PSScriptRoot\test_key_regex.py"     'test_key_regex'
# reverse-verification: prove the quote checker really catches the fatal
# "unquoted value starting with {" case (it disables the whole mod in game)
RunChecker "$PSScriptRoot\test_quote_detection.py" 'test_quote_detection'
# reverse-verification: replay a REAL captured bad load log and prove the load-test
# rules below actually fire (the "mod auto-disabled, then retried without it" case
# that shipped a completely disabled Chinese patch while the pipeline stayed green)
RunChecker "$PSScriptRoot\test_loadtest_detection.py" 'test_loadtest_detection'
# reverse-verification: park the patch's English template and prove the template
# checker fails (a missing template = Chinese silently not loaded, and the NEXT
# launch disables BOTH mods through the .legacy rename clash)
RunChecker "$PSScriptRoot\test_cn_template.py" 'test_cn_template'
RunChecker "$PSScriptRoot\sync_translation.py"   'sync_translation'
RunChecker "$PSScriptRoot\ensure_ps1_bom.py"     'ensure_ps1_bom'

# package contents: prove the Chinese is REALLY in the patch (not just "big enough")
$mainPack = Join-Path $SaveDir 'Mods\WastelandSoul.tmod'
$patchPack = Join-Path $SaveDir 'Mods\WastelandSoulCN.tmod'
if ((Test-Path $mainPack) -and (Test-Path $patchPack)) {
    RunChecker "$PSScriptRoot\check_tmod_contents.py" 'tmod_contents:main'  @($mainPack)
    RunChecker "$PSScriptRoot\check_tmod_contents.py" 'tmod_contents:patch' @($patchPack)
} else {
    Write-Host '  [SKIP] tmod_contents (no .tmod found)'
}

# ---------------------------------------------------------------- 4. loadtest
if (-not $SkipLoadTest) {
    Step '4. isolated headless load test'
    $main = Join-Path $SaveDir 'Mods\WastelandSoul.tmod'
    $patch = Join-Path $SaveDir 'Mods\WastelandSoulCN.tmod'

    # the build may have renamed a localisation file to *.legacy; a leftover would
    # make the load test itself fail (see Clear-LegacyLocalization)
    Clear-LegacyLocalization 'pre-loadtest'

    if ((Test-Path $main) -and (Test-Path $patch)) {
        Remove-Item -Recurse -Force $TestDir -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force (Join-Path $TestDir 'Mods') | Out-Null
        Copy-Item $main, $patch (Join-Path $TestDir 'Mods') -Force

        # Hard dependencies must be present in the isolated folder too, or the
        # load test fails on a missing dependency instead of on our own code.
        # (SubworldLibrary joined the list when the Fireplace subworld landed.)
        $deps = @('SubworldLibrary', 'InnoVault')
        foreach ($dep in $deps) {
            $depFile = Join-Path $RealMods "$dep.tmod"
            if (Test-Path $depFile) {
                Copy-Item $depFile (Join-Path $TestDir 'Mods') -Force
            } else {
                Write-Host ("  [WARN] dependency {0}.tmod not found in the game Mods folder" -f $dep)
            }
        }

        $enabled = @('WastelandSoul', 'WastelandSoulCN', 'BossChecklist') + $deps
        ($enabled | ConvertTo-Json) |
            Set-Content (Join-Path $TestDir 'Mods\enabled.json') -Encoding utf8

        $stamp = Get-Date -Format 'HHmmss'
        $log = Join-Path $TestDir ("out-$stamp.log")
        $proc = Start-Process -FilePath 'dotnet' `
            -ArgumentList @("$TmlDir\tModLoader.dll", '-server', '-tmlsavedirectory', $TestDir) `
            -WorkingDirectory $TmlDir -RedirectStandardOutput $log -RedirectStandardError (Join-Path $TestDir ("err-$stamp.log")) `
            -PassThru -NoNewWindow

        $deadline = (Get-Date).AddSeconds(180)
        while ((Get-Date) -lt $deadline -and -not $proc.HasExited) {
            Start-Sleep -Seconds 3
            if ((Test-Path $log) -and (Select-String -Path $log -Pattern 'Choose World' -Quiet)) { break }
        }
        if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }

        $content = Get-Content $log -Encoding UTF8 -ErrorAction SilentlyContinue
        $reached = $content | Where-Object { $_ -match 'Choose World' }

        # "Adding Content" prints the mod DISPLAY name, not the internal name, so the
        # old pattern ('WastelandSoul') never matched anything and the check was blind.
        # Read displayName straight out of build.txt instead of hardcoding it.
        $mainName  = Get-ModDisplayName $ModSource
        $patchName = Get-ModDisplayName $PatchSource
        $addedMain  = @($content | Where-Object { $mainName  -and $_ -like ("*Adding Content: " + $mainName + "*") })
        $addedPatch = @($content | Where-Object { $patchName -and $_ -like ("*Adding Content: " + $patchName + "*") })
        $unloaded = @($content | Where-Object {
            $n = $_ -replace '^\s*Unloading:\s*', ''
            ($mainName -and $n.StartsWith($mainName)) -or ($patchName -and $n.StartsWith($patchName))
        })
        # IMPORTANT: tModLoader writes some failures to <SaveDir>\server.log and
        # only a one-line pointer to stdout. A malformed localisation file is one
        # of those: the mod gets DISABLED, but stdout just references server.log.
        # Grepping only the stdout log is how a "no Chinese at all" patch shipped
        # and how a broken .hjson stayed green for several rounds.
        $serverLog = Join-Path $TestDir 'server.log'
        $serverContent = @()
        if (Test-Path $serverLog) {
            $serverContent = Get-Content $serverLog -Encoding UTF8 -ErrorAction SilentlyContinue
        }

        # Only real trouble counts: a disabled mod, a compile error, an unhandled crash,
        # a malformed localisation / content file, or a load error that tModLoader
        # answers with "The mod(s) ... have been automatically disabled" and then
        # simply retries WITHOUT the mod (that retry still reaches "Choose World",
        # which is how a completely disabled patch stayed green).
        #
        # The last three phrases are the "English template" rule: a localisation file
        # whose prefix has no matching en-US_<prefix>.hjson is renamed to .legacy and
        # its CONTENTS ARE NOT LOADED - i.e. the Chinese silently never applies, and
        # the leftover .legacy makes the NEXT load throw IOException and disable both
        # mods. That shipped twice before anyone noticed.
        $patterns = 'Disabling|error CS|Unhandled exception|malformed|failed to load|error occurred while loading|automatically disabled|IOException|doesn''t match the filename|will not be loaded|detected as a localization file'
        $bad = @($content | Where-Object { $_ -match $patterns })
        $bad += @($serverContent | Where-Object { $_ -match $patterns })

        $added = @($addedMain) + @($addedPatch)
        $added | ForEach-Object { Write-Host ("     " + $_.Trim()) }

        if (-not $addedMain.Count -or -not $addedPatch.Count) {
            Write-Host ("  [FAIL] load test: mod never reached 'Adding Content' (main={0}, patch={1}, displayNames='{2}' / '{3}')" -f `
                $addedMain.Count, $addedPatch.Count, $mainName, $patchName)
            $failed += 'loadtest:not-added'
        }
        elseif ($unloaded.Count -gt 0) {
            Write-Host '  [FAIL] load test: mod was added and then unloaded (auto-disabled)'
            $unloaded | Select-Object -First 6 | ForEach-Object { Write-Host ("     " + $_.Trim()) }
            $failed += 'loadtest:unloaded'
        }
        elseif ($reached -and $bad.Count -eq 0) {
            Write-Host '  [OK]   both mods added and loaded to "Choose World" with no errors'
        } else {
            Write-Host '  [FAIL] load test'
            $bad | Select-Object -First 10 | ForEach-Object { Write-Host ("     " + $_.Trim()) }
            $failed += 'loadtest'
        }
    } else {
        Write-Host '  [SKIP] no .tmod found'
    }
} else {
    Step '4. load test (skipped)'
}

# ---------------------------------------------------------------- 5. install
if (-not $SkipInstall) {
    Step '5. install into game Mods folder'
    # NOTE: the backup is NOT refreshed here. It was already snapshotted in step 1,
    # before the load test could rewrite the patch source file. Copying the source
    # file into the backup at this point would store an already-gutted file.
    if (-not (Test-Path $Backup)) {
        Write-Host '  [FAIL] backup snapshot missing (step 1 should have created it)'
        $failed += 'backup:missing'
    }

    foreach ($name in @('WastelandSoul.tmod', 'WastelandSoulCN.tmod')) {
        $src = Join-Path $SaveDir "Mods\$name"
        if (Test-Path $src) {
            $dest = Join-Path $RealMods $name
            $sourceHash = (Get-FileHash $src -Algorithm SHA256).Hash

            if (Test-Path $dest) {
                [void](Wait-ForModUnlocked $dest $WaitForGame)
            }

            Copy-Item $src $RealMods -Force -ErrorAction SilentlyContinue

            # VERIFY the copy: if the game is running it may hold the file and the
            # Copy-Item above fails silently (ErrorAction SilentlyContinue), which
            # used to print [OK] with the OLD file's size - i.e. it claimed an install
            # that never happened. Compare hashes instead.
            if (-not (Test-Path $dest)) {
                Write-Host ("  [FAIL] {0} not present in the game Mods folder" -f $name)
                $failed += "install:$name"
                continue
            }

            $destHash = (Get-FileHash $dest -Algorithm SHA256).Hash

            if ($sourceHash -eq $destHash) {
                $item = Get-Item $dest
                Write-Host ("  [OK]   {0}  {1} bytes  {2}  (sha256 {3})" -f `
                    $item.Name, $item.Length, $item.LastWriteTime.ToString('HH:mm:ss'), $destHash.Substring(0, 8))
            } else {
                Write-Host ("  [FAIL] {0} copy did not take effect (game running / file locked?)" -f $name)
                Write-Host ("         build output sha256 {0}, installed file sha256 {1}" -f `
                    $sourceHash.Substring(0, 8), $destHash.Substring(0, 8))
                $failed += "install:$name"
            }
        } else {
            Write-Host ("  [FAIL] missing {0}" -f $src)
            $failed += "install:$name"
        }
    }

    # restore the patch source from the pre-load snapshot (tModLoader rewrites it while loading)
    if (Test-Path $Backup) {
        Copy-Item $Backup (Join-Path $PatchSource 'Localization\zh-Hans_Mods.WastelandSoul.hjson') -Force
        Write-Host '  [OK]   restored WastelandSoulCN zh-Hans source from the pre-load snapshot'
    }
} else {
    Step '5. install (skipped)'
}

# ---------------------------------------------------------------- 6. re-sync
# tModLoader REWRITES the localisation files inside the mod sources while loading,
# and it only writes back a minimal set of keys. Keys it cannot attribute come back
# as `// commented English` lines - the BossChecklist.*.SpawnInfo values are exactly
# such a case (they are resolved at runtime through the "$key" convention, so the
# value itself need not live in the file).
#
# Such a rewritten file must NEVER become the source of truth:
#   * a backup refreshed after the load test captures the REWRITTEN file, so
#     "restoring from backup" simply restores the damage;
#   * check_cn_parity then legitimately reports those keys as missing.
# The translation table in sync_cn_translation.py is the source of truth, so we
# regenerate from it as the LAST step of every run.
Step '6. re-sync localisation after the load test'
RunChecker "$PSScriptRoot\sync_cn_translation.py" 'sync_cn_translation (post-load)'
RunChecker "$PSScriptRoot\check_cn_parity.py"     'check_cn_parity (post-load)'

# ---------------------------------------------------------------- summary
Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host 'PIPELINE RESULT: all green'
    exit 0
} else {
    Write-Host ('PIPELINE RESULT: FAILED -> ' + ($failed -join ', '))
    exit 1
}
