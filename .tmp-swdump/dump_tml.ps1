$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($args[0])
$typeNames = $args[1].Split("|")
$methods = @()
if ($args.Count -gt 2 -and $args[2]) { $methods = $args[2].Split("|") }

function Dump-Type($t) {
  Write-Output ("TYPE {0} : {1}" -f $t.FullName, $(if ($t.BaseType) { $t.BaseType.FullName } else { "" }))
  foreach ($f in $t.Fields) {
    $fa = @(); if ($f.IsPublic) { $fa += "public" } elseif ($f.IsPrivate) { $fa += "private" } else { $fa += "protected" }
    if ($f.IsStatic) { $fa += "static" }; if ($f.IsInitOnly) { $fa += "readonly" }
    Write-Output ("  FIELD {0} {1} {2}" -f ($fa -join " "), $f.FieldType.FullName, $f.Name)
  }
  foreach ($p in $t.Properties) {
    $pa = @(); if ($p.GetMethod -and $p.GetMethod.IsPublic) { $pa += "get" }; if ($p.SetMethod -and $p.SetMethod.IsPublic) { $pa += "set" }
    Write-Output ("  PROP {0} {1} {{{2}}}" -f $p.PropertyType.FullName, $p.Name, ($pa -join ","))
  }
  foreach ($m in $t.Methods) {
    if ($m.IsGetter -or $m.IsSetter) { continue }
    $ma = @(); if ($m.IsPublic) { $ma += "public" } elseif ($m.IsPrivate) { $ma += "private" } else { $ma += "protected" }
    if ($m.IsStatic) { $ma += "static" }; if ($m.IsVirtual) { $ma += "virtual" }
    $ps = @(); foreach ($pp in $m.Parameters) { $ps += ($pp.ParameterType.FullName + " " + $pp.Name) }
    Write-Output ("  METHOD {0} {1} {2}({3})" -f ($ma -join " "), $m.ReturnType.FullName, $m.Name, ($ps -join ", "))
  }
}

function Dump-Method($t, $m) {
  Write-Output ("### {0}::{1}" -f $t.FullName, $m.Name)
  if (-not $m.HasBody) { Write-Output "   (no body)"; return }
  foreach ($i in $m.Body.Instructions) {
    $op = $i.OpCode.Name
    $operand = ""
    if ($i.Operand -ne $null) {
      $o = $i.Operand
      if ($o -is [Mono.Cecil.MethodReference]) { $operand = ($o.DeclaringType.FullName + "::" + $o.Name + "(" + (($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ",") + ")") }
      elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = ($o.DeclaringType.FullName + "::" + $o.Name) }
      elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
      elseif ($o -is [string]) { $operand = '"' + $o + '"' }
      else { $operand = $o.ToString() }
    }
    Write-Output ("   IL_{0:X4}: {1} {2}" -f $i.Offset, $op, $operand)
  }
}

function WalkNs($types, $ns) {
  foreach ($t in $types) {
    if ($typeNames -contains $t.FullName) { Dump-Type $t }
    foreach ($m in $t.Methods) {
      if ($methods -contains ($t.FullName + "::" + $m.Name)) { Dump-Method $t $m }
    }
    if ($t.NestedTypes.Count -gt 0) { WalkNs $t.NestedTypes $ns }
  }
}
WalkNs $asm.MainModule.Types ""
