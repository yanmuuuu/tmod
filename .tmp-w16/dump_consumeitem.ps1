$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

function Dump-Method($t, $m) {
	Write-Output ('### {0}::{1}  (body={2})' -f $t.FullName, $m.Name, $m.HasBody)
	if (-not $m.HasBody) { return }
	foreach ($i in $m.Body.Instructions) {
		$op = $i.OpCode.Name
		$operand = ''
		if ($null -ne $i.Operand) {
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $operand = ($o.DeclaringType.Name + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = ($o.DeclaringType.Name + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
			elseif ($o -is [string]) { $operand = '"' + $o + '"' }
			else { $operand = $o.ToString() }
		}
		Write-Output ('   IL_{0:X4}: {1} {2}' -f $i.Offset, $op, $operand)
	}
}

foreach ($target in @('Terraria.Player::ConsumeItem')) {
	$parts = $target -split '::'
	$t = $modAsm.MainModule.GetType($parts[0])
	foreach ($m in $t.Methods) { if ($m.Name -eq $parts[1]) { Dump-Method $t $m } }
}
