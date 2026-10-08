# ============================================================================
#  06:00 关机守卫（由计划任务 WastelandWorkGuard 每 10 分钟调用一次）
#
#  规则（按玩家的原话：任务完成则关机；没完成就完成之后再关机）：
#    1) 完成标志 E:\开发\.work-done 不存在  -> 什么都不做（下一轮再问）
#    2) 有 tModLoader / Terraria / dotnet 在跑 -> 推迟（绝不打断玩家的游戏/存档）
#    3) 两个条件都满足 -> 发关机指令（留 2 分钟提示），然后删掉标志与计划任务（一次性）
#
#  取消方式：
#    * 只是想取消这次关机： shutdown /a
#    * 想彻底取消这个机制： schtasks /Delete /TN "WastelandWorkGuard" /F
#                            Remove-Item E:\开发\.work-done -Force
#
#  英文注释为主（PowerShell 5.1 无 BOM 会按 ANSI 读中文，本项目踩过这个坑；
#  本文件已带 UTF-8 BOM，但仍尽量少用中文）。
# ============================================================================
param(
    [string]$Marker = 'E:\开发\.work-done',
    [string]$LogFile = 'E:\开发\.shutdown-guard.log',
    [int]$GraceSeconds = 120
)

function Write-Log($message) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'yyyy/MM/dd HH:mm:ss'), $message
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

if (-not (Test-Path $Marker)) {
    Write-Log 'NOT-DONE: marker missing, keep working (no shutdown).'
    exit 0
}

$busy = @(Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.ProcessName -match 'tModLoader|Terraria|dotnet' })

if ($busy.Count -gt 0) {
    $names = ($busy | Select-Object -ExpandProperty ProcessName -Unique) -join ', '
    Write-Log ('GAME-RUNNING ({0}): postpone shutdown to the next run.' -f $names)
    exit 0
}

Write-Log ('DONE: issuing shutdown in {0}s.' -f $GraceSeconds)

shutdown.exe /s /t $GraceSeconds /c 'WastelandSoul work finished - shutting down (run "shutdown /a" to abort)'

# one-shot: drop the marker and the scheduled task so a reboot cannot re-trigger it
Remove-Item $Marker -Force -ErrorAction SilentlyContinue
schtasks.exe /Delete /TN 'WastelandWorkGuard' /F 2>$null | Out-Null

Write-Log 'Shutdown issued; marker + scheduled task removed.'
exit 0
