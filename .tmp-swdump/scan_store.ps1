$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($args[0])
$patterns = $args[1].Split(",")
$opcodes = @()
if ($args.Count -gt 2 -and $args[2]) { $opcodes = $args[2].Split(",") }

function Walk($t) {
  foreach ($m in $t.Methods) {
    if (-not $m.HasBody) { continue }
    foreach ($i in $m.Body.Instructions) {
      if ($opcodes.Count -gt 0 -and ($opcodes -notcontains $i.OpCode.Name)) { continue }
      $o = $i.Operand
      $txt = ""
      if ($o -is [Mono.Cecil.MethodReference]) { $txt = $o.DeclaringType.FullName + "::" + $o.Name }
      elseif ($o -is [Mono.Cecil.FieldReference]) { $txt = $o.DeclaringType.FullName + "::" + $o.Name }
      elseif ($o -is [Mono.Cecil.TypeReference]) { $txt = $o.FullName }
      else { continue }
      foreach ($p in $patterns) {
        if ($txt -like ("*" + $p + "*")) {
          Write-Output ("{0}::{1}  [{2}]  {3}" -f $t.FullName, $m.Name, $i.OpCode.Name, $txt)
        }
      }
    }
  }
  foreach ($n in $t.NestedTypes) { Walk $n }
}
foreach ($t in $asm.MainModule.Types) { Walk $t }
