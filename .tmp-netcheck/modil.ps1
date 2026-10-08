$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)

function Find-Type($types, $name) {
	foreach ($t in $types) {
		if ($t.FullName -eq $name) { return $t }
		if ($t.NestedTypes.Count -gt 0) {
			$r = Find-Type $t.NestedTypes $name
			if ($r) { return $r }
		}
	}
	return $null
}

$t = Find-Type $asm.MainModule.Types $typeName
if (-not $t) { Write-Output "TYPE NOT FOUND: $typeName"; exit 1 }
Write-Output ("TYPE {0}" -f $t.FullName)
foreach ($m in $t.Methods) {
	if ($methodNames -and ($methodNames -notcontains $m.Name)) { continue }
	Write-Output ("--- {0}({1})" -f $m.Name, (($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '))
	if (-not $m.HasBody) { continue }
	foreach ($i in $m.Body.Instructions) {
		$operand = ''
		if ($i.Operand -ne $null) {
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $operand = $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -is [string]) { $operand = '"' + $o + '"' }
			elseif ($o -is [Mono.Cecil.Cil.Instruction]) { $operand = ('IL_{0:X4}' -f $o.Offset) }
			else { $operand = $o.ToString() }
		}
		Write-Output ("   IL_{0:X4}: {1,-14} {2}" -f $i.Offset, $i.OpCode.Name, $operand)
	}
}
