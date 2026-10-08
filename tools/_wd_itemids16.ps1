$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("E:\steam\steamapps\common\tModLoader\tModLoader.dll")

function Find-Type($asm, $full) {
  foreach ($t in $asm.MainModule.Types) {
    if ($t.FullName -eq $full) { return $t }
    foreach ($n in $t.NestedTypes) { if ($n.FullName -eq $full) { return $n } }
  }
  return $null
}

$types = @('Terraria.ID.ItemID', 'Terraria.ID.ProjectileID', 'Terraria.ID.BuffID')

foreach ($tn in $types) {
  $t = Find-Type $asm $tn
  Write-Output ("==================== {0} ====================" -f $tn)
  if ($t -eq $null) { Write-Output "  NOT FOUND"; continue }
  Write-Output ("  fields: {0}" -f $t.Fields.Count)
  foreach ($f in $t.Fields) {
    if ($f.Name -match 'Zap|Thunder|Electric|Discharge|Gauntlet|Voltage|Charged|Charge') {
      $v = ""
      if ($f.HasConstant -and $f.Constant -ne $null) { $v = " = " + $f.Constant }
      Write-Output ("  FIELD {0} {1}{2}" -f $f.FieldType.FullName, $f.Name, $v)
    }
  }
}

Write-Output "==================== ItemID: search for zap-like near wand/staff ===================="
$item = Find-Type $asm 'Terraria.ID.ItemID'
foreach ($f in $item.Fields) {
  if ($f.Name -match 'Bolt|Beam|Wand|Staff|Tome|Rod') {
    $v = ""
    if ($f.HasConstant -and $f.Constant -ne $null) { $v = " = " + $f.Constant }
    Write-Output ("  {0}{1}" -f $f.Name, $v)
  }
}
