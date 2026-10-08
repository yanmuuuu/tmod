param(
  [Parameter(Mandatory=$true)][string]$Asm,
  [Parameter(Mandatory=$true)][string]$Out,
  [Parameter(Mandatory=$true)][string[]]$MemberPatterns,
  [string[]]$TypePatterns = @('*'),
  [int]$MaxHits = 400
)

$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Asm)
$lines = New-Object System.Collections.Generic.List[string]
$hits = 0

function Walk($t) {
  if ($script:hits -ge $MaxHits) { return }
  $typeOk = $false
  foreach ($p in $TypePatterns) { if ($t.FullName -like $p) { $typeOk = $true; break } }
  if ($typeOk) {
    foreach ($m in $t.Methods) {
      if ($script:hits -ge $MaxHits) { break }
      if (-not $m.HasBody) { continue }
      foreach ($i in $m.Body.Instructions) {
        $o = $i.Operand
        if ($null -eq $o) { continue }
        $name = $null
        if ($o -is [Mono.Cecil.MemberReference]) { $name = $o.DeclaringType.FullName + "::" + $o.Name }
        if ($null -eq $name) { continue }
        foreach ($p in $MemberPatterns) {
          if ($name -like $p) {
            $lines.Add(("{0}::{1}  @IL_{2:X4}  {3} {4}" -f $t.FullName, $m.Name, $i.Offset, $i.OpCode.Name, $name))
            $script:hits++
            break
          }
        }
      }
    }
  }
  foreach ($n in $t.NestedTypes) { Walk $n }
}

foreach ($t in $assembly.MainModule.Types) {
  if ($t.Name -eq "<Module>") { continue }
  Walk $t
}

[System.IO.File]::WriteAllLines($Out, $lines, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("hits: {0} -> {1}" -f $hits, $Out)
