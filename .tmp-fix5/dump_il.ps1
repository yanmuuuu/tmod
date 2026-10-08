param(
  [Parameter(Mandatory=$true)][string]$Asm,
  [Parameter(Mandatory=$true)][string]$Out,
  [Parameter(Mandatory=$true)][string[]]$Targets
)

$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Asm)
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("ASSEMBLY " + $assembly.FullName)
$lines.Add("LOADED in " + $sw.ElapsedMilliseconds + " ms")

function Fmt-Operand($o) {
  if ($null -eq $o) { return "" }
  if ($o -is [Mono.Cecil.MethodReference]) {
    $ps = ($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ","
    return ($o.DeclaringType.FullName + "::" + $o.Name + "(" + $ps + ")")
  }
  if ($o -is [Mono.Cecil.FieldReference]) { return ($o.DeclaringType.FullName + "::" + $o.Name) }
  if ($o -is [Mono.Cecil.TypeReference]) { return $o.FullName }
  if ($o -is [string]) { return ('"' + $o + '"') }
  return $o.ToString()
}

function Dump-Method($t, $m) {
  $lines.Add("")
  $lines.Add(("### {0}::{1}" -f $t.FullName, $m.Name))
  if (-not $m.HasBody) { $lines.Add("   (no body)"); return }
  foreach ($v in $m.Body.Variables) { $lines.Add(("   LOCAL {0} {1}" -f $v.VariableType.FullName, $v.Index)) }
  foreach ($i in $m.Body.Instructions) {
    $lines.Add(("   IL_{0:X4}: {1} {2}" -f $i.Offset, $i.OpCode.Name, (Fmt-Operand $i.Operand)))
  }
}

function Walk($t) {
  foreach ($m in $t.Methods) {
    $key = $t.FullName + "::" + $m.Name
    foreach ($pat in $Targets) {
      if ($key -like $pat) { Dump-Method $t $m; break }
    }
  }
  foreach ($n in $t.NestedTypes) { Walk $n }
}

foreach ($t in $assembly.MainModule.Types) {
  if ($t.Name -eq "<Module>") { continue }
  Walk $t
}

[System.IO.File]::WriteAllLines($Out, $lines, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("WROTE {0} lines -> {1} ({2} ms)" -f $lines.Count, $Out, $sw.ElapsedMilliseconds)
