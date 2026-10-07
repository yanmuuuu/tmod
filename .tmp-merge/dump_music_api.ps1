$ErrorActionPreference = 'Stop'
# 从 tModLoader.dll 里把 MusicLoader 用到的字符串常量打出来 —— 看它认哪些扩展名。
# （反编译/IL 只用来"猜方向"，结论以这里的字面量为准：扩展名判断就是字符串比较。）
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("E:\steam\steamapps\common\tModLoader\tModLoader.dll")
$found = @()
foreach ($t in $asm.MainModule.Types) {
    if ($t.FullName -notmatch 'MusicLoader') { continue }
    Write-Output ("TYPE " + $t.FullName)
    foreach ($m in $t.Methods) {
        if (-not $m.HasBody) { continue }
        foreach ($i in $m.Body.Instructions) {
            if ($i.OpCode.Name -eq 'ldstr') { $found += ("  {0} :: {1}" -f $m.Name, $i.Operand) }
        }
    }
}
$found | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
Write-Output "==================== 全库里出现 .ogg/.mp3/.wav 的地方（类型.方法） ===================="
foreach ($t in $asm.MainModule.Types) {
    foreach ($m in $t.Methods) {
        if (-not $m.HasBody) { continue }
        foreach ($i in $m.Body.Instructions) {
            if ($i.OpCode.Name -ne 'ldstr') { continue }
            $s = [string]$i.Operand
            if ($s -match '^\.(ogg|mp3|wav)$' -or $s -match 'Music/') {
                Write-Output ("  {0}::{1}  ->  {2}" -f $t.Name, $m.Name, $s)
            }
        }
    }
}
