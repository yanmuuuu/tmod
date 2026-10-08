# -*- coding: utf-8 -*-
"""给 scheduled_work.ps1 加一道"先探编译"的闸门：只有整树 0/0 才跑流水线。

起因：18:03 窗口一开就跑流水线，正好撞上两个代理改代码 → build / check_localization / verify_batch16 全红，
主模组没装上、白跑一轮。现在改成先在自己的独立目录里探一次编译，不干净就记一行跳过。
"""
import io

PATH = r"E:\开发\tools\scheduled_work.ps1"

PRECHECK = r"""Write-Log ('window open at {0} - probing build first' -f (Get-Date -Format 'HH:mm:ss'))

# 探测编译：只有干净才继续（避免撞上代理的在制品）
$probeMods = Join-Path 'E:\开发\.tml-build-w17' 'Mods'
New-Item -ItemType Directory -Force $probeMods | Out-Null

foreach ($name in @('InnoVault.tmod', 'WastelandSoulCN.tmod')) {
    $src = Join-Path 'E:\开发\.tml-build\Mods' $name
    $dst = Join-Path $probeMods $name

    if ((Test-Path $src) -and -not (Test-Path $dst)) {
        Copy-Item $src $dst -Force -ErrorAction SilentlyContinue
    }
}

$probe = & dotnet 'e:\steam\steamapps\common\tModLoader\tModLoader.dll' -build 'E:\开发\WastelandSoul' -tmlsavedirectory 'E:\开发\.tml-build-w17' 2>&1
$probeLine = ($probe | Select-String -Pattern 'Compilation finished' | Select-Object -Last 1)

if (-not ($probeLine -match '0 errors and 0 warnings')) {
    Write-Log ('in-progress tree (build not clean): {0} - skip this round.' -f $probeLine)
    exit 0
}

Write-Log 'tree compiles clean - running full pipeline'
"""

text = io.open(PATH, encoding="utf-8-sig").read()

if "probing build first" in text:
    print("已经有闸门，跳过")
else:
    start = text.index("Write-Log ('window open at")
    end = text.index("$pipeline = & powershell")
    text = text[:start] + PRECHECK + "\n" + text[end:]
    io.open(PATH, "w", encoding="utf-8-sig", newline="\r\n").write(text)
    print("已加装'先探编译'闸门")

print("校验:", "probing build first" in io.open(PATH, encoding="utf-8-sig").read())
