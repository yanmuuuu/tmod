$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

function F($ts, $n) {
	foreach ($t in $ts) {
		if ($t.Name -eq $n) { return $t }
		if ($t.NestedTypes.Count -gt 0) { $r = F $t.NestedTypes $n; if ($r) { return $r } }
	}
	return $null
}

$t = F $asm.MainModule.Types 'ModPacket'
Write-Output "TYPE $($t.FullName) : $($t.BaseType.FullName)"
foreach ($m in $t.Methods) { Write-Output "  METHOD $($m.ReturnType.Name) $($m.Name)($(($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '))" }
foreach ($m in $t.Methods) {
	if (-not $m.HasBody) { continue }
	Write-Output "--- $($m.Name)"
	foreach ($i in $m.Body.Instructions) {
		$o = $i.Operand
		$operand = ''
		if ($o -is [Mono.Cecil.MethodReference]) { $operand = $o.DeclaringType.Name + '::' + $o.Name + '(' + (($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ')' }
		elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = $o.DeclaringType.Name + '::' + $o.Name }
		elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
		elseif ($o -is [string]) { $operand = '"' + $o + '"' }
		elseif ($o -is [Mono.Cecil.Cil.Instruction]) { $operand = ('IL_{0:X4}' -f $o.Offset) }
		elseif ($o -ne $null) { $operand = $o.ToString() }
		Write-Output ("   IL_{0:X4}: {1,-14} {2}" -f $i.Offset, $i.OpCode.Name, $operand)
	}
}
