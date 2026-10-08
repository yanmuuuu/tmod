param(
  [Parameter(Mandatory=$true)][string]$Asm,
  [Parameter(Mandatory=$true)][string]$TypeName,
  [Parameter(Mandatory=$true)][string]$Out,
  [string[]]$Patterns = @('*')
)
$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Asm)
$lines = New-Object System.Collections.Generic.List[string]
function Walk($t) {
  if ($t.FullName -eq $TypeName -or $t.Name -eq $TypeName) {
    foreach ($f in $t.Fields) {
      if ($f.Name -eq "value__") { continue }
      $ok = $false
      foreach ($p in $Patterns) { if ($f.Name -like $p) { $ok = $true; break } }
      if (-not $ok) { continue }
      $lines.Add(("{0} = {1}" -f $f.Name, $f.Constant))
    }
  }
  foreach ($n in $t.NestedTypes) { Walk $n }
}
foreach ($t in $assembly.MainModule.Types) { Walk $t }
[System.IO.File]::WriteAllLines($Out, $lines, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("WROTE {0}" -f $lines.Count)
