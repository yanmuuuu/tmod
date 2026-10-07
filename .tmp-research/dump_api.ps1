$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
$dll = "E:\开发\.tmp-research\inno\InnoVault.dll"
if (-not (Test-Path $cecil)) { Write-Output "CECIL MISSING: $cecil"; exit 1 }
if (-not (Test-Path $dll)) { Write-Output "DLL MISSING: $dll"; exit 1 }

Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)

Write-Output "==== ASSEMBLY: $($asm.Name.FullName) ===="
Write-Output "==== RUN-TIME VERSION (target framework) ===="
Write-Output $asm.MainModule.RuntimeVersion
Write-Output "==== ASSEMBLY REFS ===="
foreach ($r in $asm.MainModule.AssemblyReferences) { Write-Output ("  {0} {1}" -f $r.Name, $r.Version) }

Write-Output ""
Write-Output "==== PUBLIC TYPES in InnoVault.Models3D.* ===="

function Get-Vis {
  param($t)
  if ($t.IsPublic -or $t.IsNestedPublic) { return "public" }
  if ($t.IsNestedFamily) { return "protected" }
  return "internal"
}

$all = New-Object System.Collections.Generic.List[object]
function Walk($t) {
  $all.Add($t)
  foreach ($n in $t.NestedTypes) { Walk $n }
}
foreach ($t in $asm.MainModule.Types) { if ($t.Name -ne "<Module>") { Walk $t } }

foreach ($t in $all) {
  if ($t.FullName -notlike "InnoVault.Models3D*") { continue }
  $vis = Get-Vis $t
  if ($vis -ne "public") { continue }
  $kind = "class"
  if ($t.IsInterface) { $kind = "interface" } elseif ($t.IsEnum) { $kind = "enum" } elseif ($t.IsValueType) { $kind = "struct" }
  $base = ""
  if ($t.BaseType) { $base = " : " + $t.BaseType.FullName }
  Write-Output ""
  Write-Output ("[{0}] {1} {2}{3}" -f $vis, $kind, $t.FullName, $base)

  if ($t.IsEnum) {
    foreach ($f in $t.Fields) { if ($f.IsLiteral) { Write-Output ("        {0} = {1}" -f $f.Name, $f.Constant) } }
    continue
  }

  foreach ($p in $t.Properties) {
    $acc = @()
    if ($p.GetMethod -and $p.GetMethod.IsPublic) { $acc += "get" }
    if ($p.SetMethod -and $p.SetMethod.IsPublic) { $acc += "set" }
    if ($acc.Count -eq 0) { continue }
    Write-Output ("    PROP  {0} {1} {{{2}}}" -f $p.PropertyType.FullName, $p.Name, ($acc -join ","))
  }
  foreach ($f in $t.Fields) {
    if (-not $f.IsPublic -or $f.IsLiteral) { continue }
    Write-Output ("    FIELD {0} {1}" -f $f.FieldType.FullName, $f.Name)
  }
  foreach ($m in $t.Methods) {
    if (-not $m.IsPublic -or $m.IsGetter -or $m.IsSetter) { continue }
    if ($m.Name -like "get_*" -or $m.Name -like "set_*") { continue }
    $st = ""
    if ($m.IsStatic) { $st = "static " }
    $ps = @()
    foreach ($pp in $m.Parameters) { $ps += ($pp.ParameterType.FullName + " " + $pp.Name) }
    Write-Output ("    {0}{1} {2}({3})" -f $st, $m.ReturnType.FullName, $m.Name, ($ps -join ", "))
  }
}
Write-Output ""
Write-Output "==== VaultLoadenHandle base + Model3DLoadenHandle registration ===="
foreach ($t in $all) {
  if ($t.FullName -eq "InnoVault.VaultLoadenHandle" -or $t.FullName -eq "InnoVault.Models3D.Runtime.Model3DLoadenHandle") {
    Write-Output ("TYPE {0} : {1}" -f $t.FullName, $t.BaseType.FullName)
  }
}
Write-Output ""
Write-Output "==== TEST: does InnoVault mention .glb anywhere in DLL strings? ===="
$bytes = [System.IO.File]::ReadAllBytes($dll)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
foreach ($kw in @('.glb', 'glb', 'FBX', '.fbx', 'gltf', '.gltf', '.obj')) {
  $c = ([regex]::Matches($ascii, [regex]::Escape($kw))).Count
  Write-Output ("  {0,-8} occurrences={1}" -f $kw, $c)
}
