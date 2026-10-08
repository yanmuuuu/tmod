param(
  [Parameter(Mandatory=$true)][string]$Asm,
  [Parameter(Mandatory=$true)][string]$Out,
  [string[]]$TypePatterns = @('*'),
  [string[]]$NamePatterns = @('*')
)

$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Asm)
$lines = New-Object System.Collections.Generic.List[string]

function Emit-Type($t) {
  $hit = $false
  foreach ($p in $TypePatterns) { if ($t.FullName -like $p) { $hit = $true; break } }
  if (-not $hit) { return }
  if ($t.IsEnum) {
    $lines.Add("ENUM " + $t.FullName)
    foreach ($f in $t.Fields) {
      if ($f.Name -eq "value__") { continue }
      $lines.Add(("   {0} = {1}" -f $f.Name, $f.Constant))
    }
    return
  }
  $lines.Add("TYPE " + $t.FullName + " : " + $t.BaseType)
  foreach ($m in $t.Methods) {
    if ($m.IsGetter -or $m.IsSetter) { continue }
    $ok = $false
    foreach ($p in $NamePatterns) { if ($m.Name -like $p) { $ok = $true; break } }
    if (-not $ok) { continue }
    $ps = ($m.Parameters | ForEach-Object { $_.ParameterType.Name + " " + $_.Name }) -join ", "
    $lines.Add(("   M {0} {1}({2})" -f $m.ReturnType.Name, $m.Name, $ps))
  }
}

function Walk($t) {
  if ($t.Name -ne "<Module>") { Emit-Type $t }
  foreach ($n in $t.NestedTypes) { Walk $n }
}

foreach ($t in $assembly.MainModule.Types) { Walk $t }

[System.IO.File]::WriteAllLines($Out, $lines, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("WROTE {0} lines -> {1}" -f $lines.Count, $Out)
