$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

# 只扫这几个类型：找 "ldc.i4 55" 紧邻 SendData 的位置（= 谁在发 MessageID.PlayerBuffs）
$typeNames = @('Terraria.NetMessage', 'Terraria.MessageBuffer', 'Terraria.Player', 'Terraria.NPC', 'Terraria.Main')

foreach ($tn in $typeNames) {
	$t = $modAsm.MainModule.GetType($tn)
	if ($null -eq $t) { Write-Output ('!! missing ' + $tn); continue }
	foreach ($m in $t.Methods) {
		if (-not $m.HasBody) { continue }
		$ins = @($m.Body.Instructions)
		for ($k = 0; $k -lt $ins.Count; $k++) {
			$i = $ins[$k]
			if ($i.OpCode.Name -eq 'ldc.i4.s' -and "$($i.Operand)" -eq '55') {
				$window = @()
				for ($j = $k; $j -lt [Math]::Min($k + 12, $ins.Count); $j++) {
					$o = $ins[$j].Operand
					$txt = $ins[$j].OpCode.Name
					if ($o -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
					elseif ($o -ne $null) { $txt += ' ' + $o.ToString() }
					$window += $txt
				}
				Write-Output ('[{0}::{1}] @IL_{2:X4}: {3}' -f $t.Name, $m.Name, $i.Offset, ($window -join ' | '))
			}
		}
	}
}
