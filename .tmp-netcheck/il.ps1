$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$want = @{}
foreach ($spec in $targets) { $want[$spec] = $true }

function Emit-Method($t, $m) {
	Write-Output ("### {0}::{1}" -f $t.FullName, $m.Name)
	if (-not $m.HasBody) { Write-Output '   (no body)'; return }	foreach ($h in $m.Body.ExceptionHandlers) {
		Write-Output ("   EH {0} try IL_{1:X4}-IL_{2:X4} handler IL_{3:X4}-IL_{4:X4} catch={5}" -f `
			$h.HandlerType, $h.TryStart.Offset, $h.TryEnd.Offset, $h.HandlerStart.Offset, $h.HandlerEnd.Offset, `
			$(if ($h.CatchType) { $h.CatchType.FullName } else { '' }))
	}
	foreach ($i in $m.Body.Instructions) {
		$op = $i.OpCode.Name
		$operand = ''
		if ($i.Operand -ne $null) {
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) {
				$ps = ($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','
				$operand = $o.DeclaringType.FullName + '::' + $o.Name + '(' + $ps + ')'
			}
			elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = $o.DeclaringType.FullName + '::' + $o.Name }
			elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
			elseif ($o -is [string]) { $operand = '"' + $o + '"' }
			elseif ($o -is [Mono.Cecil.Cil.Instruction]) { $operand = ('IL_{0:X4}' -f $o.Offset) }
			else { $operand = $o.ToString() }
		}
		Write-Output ("   IL_{0:X4}: {1,-14} {2}" -f $i.Offset, $op, $operand)
	}
}

function Walk($types) {
	foreach ($t in $types) {
		foreach ($m in $t.Methods) {
			$key = $t.FullName + '::' + $m.Name
			if ($want.ContainsKey($key)) { Emit-Method $t $m }
		}
		if ($t.NestedTypes.Count -gt 0) { Walk $t.NestedTypes }
	}
}
Walk $asm.MainModule.Types
