$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$mb = $modAsm.MainModule.GetType('Terraria.MessageBuffer')
Write-Output ('MessageBuffer methods: ' + ($mb.Methods.Count))

foreach ($m in $mb.Methods) {
	if (-not $m.HasBody) { continue }
	$ins = @($m.Body.Instructions)
	for ($k = 0; $k -lt $ins.Count; $k++) {
		$o = $ins[$k].Operand
		if ($o -is [Mono.Cecil.MethodReference] -and $o.Name -in @('AddBuff', 'AddBuff_ActuallyTryToAddTheBuff', 'AddBuff_TryUpdatingExistingBuffTime')) {
			Write-Output ('=== {0}::{1} @IL_{2:X4} calls {3}' -f $mb.Name, $m.Name, $ins[$k].Offset, $o.Name)
			$from = [Math]::Max(0, $k - 45)
			$to = [Math]::Min($ins.Count - 1, $k + 6)
			for ($j = $from; $j -le $to; $j++) {
				$ii = $ins[$j]
				$txt = $ii.OpCode.Name
				$oo = $ii.Operand
				if ($oo -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $oo.DeclaringType.Name + '::' + $oo.Name }
				elseif ($oo -is [Mono.Cecil.FieldReference]) { $txt += ' ' + $oo.DeclaringType.Name + '::' + $oo.Name }
				elseif ($oo -ne $null) { $txt += ' ' + $oo.ToString() }
				Write-Output ('   IL_{0:X4}: {1}' -f $ii.Offset, $txt)
			}
		}
	}
}
