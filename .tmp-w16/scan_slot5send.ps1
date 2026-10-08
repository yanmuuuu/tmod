$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$t = $modAsm.MainModule.GetType('Terraria.Player')
foreach ($m in $t.Methods) {
	if (-not $m.HasBody) { continue }
	$ins = @($m.Body.Instructions)
	for ($k = 0; $k -lt $ins.Count; $k++) {
		if ($ins[$k].OpCode.Name -ne 'ldc.i4.5') { continue }
		$found = $false
		for ($j = $k; $j -lt [Math]::Min($k + 14, $ins.Count); $j++) {
			$o = $ins[$j].Operand
			if ($o -is [Mono.Cecil.MethodReference] -and $o.Name -eq 'SendData') { $found = $true; break }
		}
		if (-not $found) { continue }
		Write-Output ('--- Player::{0} @IL_{1:X4}' -f $m.Name, $ins[$k].Offset)
		for ($j = $k; $j -le [Math]::Min($ins.Count - 1, $k + 12); $j++) {
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
