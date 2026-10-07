# 废土魂穿：构建 + 无头加载自检
#
# 为什么需要它：tModLoader 命令行的 -build 参数是「路径」（按当前目录解析），不是模组名。
# 只写 -build <模组名> 会去编译 <安装目录>\<模组名>，若那里不是你的工程就会编译出
# 一个空程序集，还照样打印 "Compilation finished with 0 errors"，产出的 .tmod 只有 9KB、
# 没有贴图、加载时报「命名空间与文件夹名不匹配」。本脚本强制用绝对路径并校验体积。
#
# 用法（Windows PowerShell 5.1 默认执行策略是 Restricted，需要 Bypass）：
#   powershell -NoProfile -ExecutionPolicy Bypass -File E:\开发\tools\build_and_loadtest.ps1
#   可选参数：
#     -ModSource <工程目录>   默认 E:\开发\WastelandSoul
#     -TmlDir    <安装目录>   默认 E:\steam\steamapps\common\tModLoader
#     -SaveDir   <存档目录>   留空则用默认存档目录（.tmod 会装进你的 Mods 里）
#     -TestDir   <自检目录>   默认 %TEMP%\WastelandSoul-loadtest
#
# 注意：本文件必须保存为「UTF-8 带 BOM」，否则 PowerShell 5.1 会按 ANSI 读取，中文注释会让脚本解析失败。

param(
	[string]$ModSource = 'E:\开发\WastelandSoul',
	[string]$TmlDir = 'E:\steam\steamapps\common\tModLoader',
	[string]$SaveDir = '',
	[string]$TestDir = (Join-Path $env:TEMP 'WastelandSoul-loadtest'),
	[int]$LoadTestSeconds = 120,
	[int]$MinTmodBytes = 20000
)

$ErrorActionPreference = 'Stop'
# PowerShell 5.1 控制台默认不是 UTF-8，不设这行中文日志会显示成乱码
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$modName = Split-Path $ModSource -Leaf
if ($SaveDir) {
	$tmod = Join-Path $SaveDir "Mods\$modName.tmod"
} else {
	$tmod = Join-Path $env:USERPROFILE "Documents\My Games\Terraria\tModLoader\Mods\$modName.tmod"
}

Write-Host "=== 1/3 构建（cwd = tModLoader 安装目录，参数 = 工程绝对路径）===" -ForegroundColor Cyan
if (-not (Test-Path $TmlDir)) { throw "找不到 tModLoader 安装目录: $TmlDir" }
if (-not (Test-Path $ModSource)) { throw "找不到工程目录: $ModSource" }

Push-Location $TmlDir
if ($SaveDir) {
	& dotnet (Join-Path $TmlDir 'tModLoader.dll') -build $ModSource -tmlsavedirectory $SaveDir
} else {
	& dotnet (Join-Path $TmlDir 'tModLoader.dll') -build $ModSource
}
$buildExit = $LASTEXITCODE
Pop-Location
if ($buildExit -ne 0) { throw "构建失败（exit $buildExit）" }

Write-Host "`n=== 2/3 产物自检 ===" -ForegroundColor Cyan
if (-not (Test-Path $tmod)) { throw "没有产出 $tmod" }
$info = Get-Item $tmod
"{0}  {1:N0} 字节  {2}" -f $info.Name, $info.Length, $info.LastWriteTime
if ($info.Length -lt $MinTmodBytes) {
	Write-Warning "体积偏小（< $MinTmodBytes 字节）：很可能又编译成了空目录。确认传的是绝对路径，且输出里能看到你源码的警告/报错。"
}

Write-Host "`n=== 3/3 无头加载自检（隔离存档目录，不会碰你的存档）===" -ForegroundColor Cyan
Remove-Item -Recurse -Force $TestDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path (Join-Path $TestDir 'Mods') -Force | Out-Null
Copy-Item $tmod (Join-Path $TestDir 'Mods') -Force
# 只启用这一个模组，加载快、且不受其它模组报错干扰
Set-Content -Path (Join-Path $TestDir 'Mods\enabled.json') -Value "[`"$modName`"]" -Encoding utf8

$log = Join-Path $TestDir 'load.log'
$errLog = Join-Path $TestDir 'load.err.log'
$proc = Start-Process -FilePath 'dotnet' `
	-ArgumentList @((Join-Path $TmlDir 'tModLoader.dll'), '-server', '-tmlsavedirectory', $TestDir) `
	-WorkingDirectory $TmlDir -RedirectStandardOutput $log -RedirectStandardError $errLog -PassThru -NoNewWindow

# 加载完成后服务端会停在「Choose World:」等输入，看到标记就收工
$deadline = (Get-Date).AddSeconds($LoadTestSeconds)
while ((Get-Date) -lt $deadline -and -not $proc.HasExited) {
	Start-Sleep -Seconds 2
	if ((Test-Path $log) -and (Select-String -Path $log -Pattern 'Choose World|Adding Recipes' -Quiet)) { break }
}
if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }

$text = ''
if (Test-Path $log) { $text = Get-Content $log -Raw -Encoding UTF8 }
if (Test-Path $errLog) { $text += "`n" + (Get-Content $errLog -Raw -Encoding UTF8) }

$contentOk = $text -match 'Adding Recipes'
$failed = $text -match '该模组加载时发生错误|error CS\d|Unhandled exception|Disabling Mod'

Write-Host "`n--- 加载日志（关键行）---"
($text -split "`r?`n" | Where-Object { $_ -match 'Adding Content|Configuring|Finalizing|Adding Recipes|Choose World|诊断|ERROR|错误|Disabling' }) |
	Select-Object -First 25 | ForEach-Object { "  $_" }

if ($failed -or -not $contentOk) {
	Write-Host "`n结果：加载自检失败" -ForegroundColor Red
	if (-not $contentOk) { Write-Host "  未看到内容注册完成标记（Adding Recipes / Choose World）" }
	Write-Host "  完整日志：$log"
	exit 1
}

Write-Host "`n结果：构建 + 加载自检全部通过" -ForegroundColor Green
Write-Host "  完整日志：$log"
exit 0
