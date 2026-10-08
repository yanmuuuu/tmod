$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$path = $args[0]
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)

Write-Output "==================== ASSEMBLY REFS ===================="
foreach ($r in $asm.MainModule.AssemblyReferences) { Write-Output ("REF: {0} {1}" -f $r.Name, $r.Version) }

function Dump-Type($t, $indent) {
  $pad = " " * $indent
  $attrs = @()
  if ($t.IsPublic) { $attrs += "public" } elseif ($t.IsNotPublic) { $attrs += "internal" }
  if ($t.IsAbstract -and $t.IsSealed) { $attrs += "static" }
  elseif ($t.IsAbstract) { $attrs += "abstract" }
  elseif ($t.IsSealed) { $attrs += "sealed" }
  if ($t.IsInterface) { $attrs += "interface" }
  $base = ""
  if ($t.BaseType) { $base = " : " + $t.BaseType.FullName }
  Write-Output ("{0}TYPE [{1}] {2}{3}" -f $pad, ($attrs -join " "), $t.FullName, $base)

  foreach ($f in $t.Fields) {
    $fa = @()
    if ($f.IsPublic) { $fa += "public" } elseif ($f.IsPrivate) { $fa += "private" } else { $fa += "protected" }
    if ($f.IsStatic) { $fa += "static" }
    if ($f.IsInitOnly) { $fa += "readonly" }
    if ($f.IsLiteral) { $fa += "const" }
    $v = ""
    if ($f.HasConstant -and $f.Constant -ne $null) { $v = " = " + $f.Constant }
    Write-Output ("{0}  FIELD {1} {2} {3}{4}" -f $pad, ($fa -join " "), $f.FieldType.FullName, $f.Name, $v)
  }
  foreach ($p in $t.Properties) {
    $pa = @()
    if ($p.GetMethod -and $p.GetMethod.IsPublic) { $pa += "get" }
    if ($p.SetMethod -and $p.SetMethod.IsPublic) { $pa += "set" }
    Write-Output ("{0}  PROP {1} {2} {{{3}}}" -f $pad, $p.PropertyType.FullName, $p.Name, ($pa -join ","))
  }
  foreach ($m in $t.Methods) {
    if ($m.IsGetter -or $m.IsSetter) { continue }
    $ma = @()
    if ($m.IsPublic) { $ma += "public" } elseif ($m.IsPrivate) { $ma += "private" } else { $ma += "protected" }
    if ($m.IsStatic) { $ma += "static" }
    if ($m.IsVirtual -and -not $m.IsNewSlot) { $ma += "override" } elseif ($m.IsVirtual) { $ma += "virtual" }
    if ($m.IsAbstract) { $ma += "abstract" }
    $ps = @()
    foreach ($pp in $m.Parameters) { $ps += ($pp.ParameterType.FullName + " " + $pp.Name) }
    Write-Output ("{0}  METHOD {1} {2} {3}({4})" -f $pad, ($ma -join " "), $m.ReturnType.FullName, $m.Name, ($ps -join ", "))
  }
  foreach ($n in $t.NestedTypes) { Dump-Type $n ($indent + 2) }
}

foreach ($t in $asm.MainModule.Types) {
  if ($t.Name -eq "<Module>") { continue }
  Dump-Type $t 0
}
