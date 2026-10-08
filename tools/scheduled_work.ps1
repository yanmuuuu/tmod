# ============================================================================
#  按时开工：等到「非高峰时段」后自动跑一轮完整验收+装机
#
#  依据 开发说明.md 开头的「〇、开发排期大前提」：
#    08:00-12:00 与 14:00-18:00 禁止开工；其余时段正常。
#
#  本脚本只做**确定性**的事（不写代码）：编译 + 全部静态检查 + 加载自检 + 装机 + 刷新验收报告。
#  编写/设计类工作由会话内的我完成（会话一旦在允许时段活跃，我就自己接着干，不需要玩家催）。
#
#  用法：powershell -NoProfile -ExecutionPolicy Bypass -File tools\scheduled_work.ps1
#  日志：E:\开发\.scheduled-work.log
# ============================================================================
param(
    [string]$LogFile = 'E:\开发\.scheduled-work.log',
    [int]$PollSeconds = 300
)

function Write-Log($message) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'yyyy/MM/dd HH:mm:ss'), $message
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Test-Peak([datetime]$time) {
    $hour = $time.Hour
    return (($hour -ge 8 -and $hour -lt 12) -or ($hour -ge 14 -and $hour -lt 18))
}

Write-Log 'scheduled_work started; waiting for an allowed window (avoid 08-12 and 14-18).'

while (Test-Peak (Get-Date)) {
    Start-Sleep -Seconds $PollSeconds
}

Write-Log ('window open at {0} - running pipeline' -f (Get-Date -Format 'HH:mm:ss'))

$pipeline = & powershell -NoProfile -ExecutionPolicy Bypass -File 'E:\开发\tools\ws_pipeline.ps1' -WaitForGame 900 2>&1
$result = ($pipeline | Select-String -Pattern 'PIPELINE RESULT' | Select-Object -Last 1)

Add-Content -Path $LogFile -Value ($pipeline | Select-Object -Last 12) -Encoding UTF8
Write-Log ('pipeline done: {0}' -f $result)

& 'C:\Users\p老师\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe' 'E:\开发\tools\make_handoff_report.py' 2>&1 |
    Add-Content -Path $LogFile -Encoding UTF8

Write-Log 'handoff report refreshed; scheduled_work finished.'
