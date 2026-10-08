$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

function Show-Type([string]$name, [switch]$bodies) {
	$found = $null
	function Search($types) {
		foreach ($t in $types) {
			if ($t.FullName -eq $name) { $script:found = $t }
			if ($t.NestedTypes.Count -gt 0) { Search $t.NestedTypes }
		}
	}
	Search $asm.MainModule.Types
	if (-not $script:found) { Write-Output "NOT FOUND $name"; return }
	$t = $script:found
	Write-Output ("TYPE {0} : {1}" -f $t.FullName, $(if ($t.BaseType) { $t.BaseType.FullName } else { '' }))
	foreach ($m in $t.Methods) { Write-Output ("   METHOD {0} {1} {2}" -f $m.ReturnType.FullName, $m.Name, (($m.Parameters | ForEach-Object { $_.ParameterType.FullName + ' ' + $_.Name }) -join ', ')) }
	if ($bodies) {
		foreach ($m in $t.Methods) {
			if (-not $m.HasBody) { continue }
			Write-Output ("--- {0}" -f $m.Name)
			foreach ($i in $m.Body.Instructions) {
				$operand = ''
				if ($i.Operand -ne $null) {
					$o = $i.Operand
					if ($o -is [Mono.Cecil.MethodReference]) { $operand = $o.DeclaringType.FullName + '::' + $o.Name }
					elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = $o.DeclaringType.FullName + '::' + $o.Name }
					elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
					elseif ($o -is [string]) { $operand = '"' + $o + '"' }
					elseif ($o -is [Mono.Cecil.Cil.Instruction]) { $operand = ('IL_{0:X4}' -f $o.Offset) }
					else { $operand = $o.ToString() }
				}
				Write-Output ("   IL_{0:X4}: {1,-14} {2}" -f $i.Offset, $i.OpCode.Name, $operand)
			}
		}
	}
}

Show-Type $target
