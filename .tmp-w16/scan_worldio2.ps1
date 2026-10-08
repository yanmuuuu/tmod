$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

function Dump-Windows($typeName, $methodName, $targetName, $before, $after) {
	$t = $modAsm.MainModule.GetType($typeName)
	foreach ($m in $t.Methods) {
		if ($m.Name -ne $methodName) { continue }
		$ins = @($m.Body.Instructions)
		for ($k = 0; $k -lt $ins.Count; $k++) {
			$o = $ins[$k].Operand
			if (-not ($o -is [Mono.Cecil.MethodReference] -and $o.Name -eq $targetName)) { continue }
			Write-Output ('--- {0}::{1} -> {2} @IL_{3:X4}' -f $t.Name, $m.Name, $targetName, $ins[$k].Offset)
			for ($j = [Math]::Max(0, $k - $before); $j -le [Math]::Min($ins.Count - 1, $k + $after); $j++) {
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

Write-Output '===== NetMessage::SendData ====='
Dump-Windows 'Terraria.NetMessage' 'SendData' 'SendModData' 12 4
Write-Output '===== MessageBuffer::GetData ====='
Dump-Windows 'Terraria.MessageBuffer' 'GetData' 'ReceiveModData' 12 4
