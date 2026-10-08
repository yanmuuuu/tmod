$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$t = $modAsm.MainModule.GetType('Terraria.MessageBuffer')
$m = $t.Methods | Where-Object { $_.Name -eq 'GetData' }
$ins = @($m.Body.Instructions)

function Show-Range($ins, $lo, $hi) {
	for ($j = 0; $j -lt $ins.Count; $j++) {
		$off = $ins[$j].Offset
		if ($off -lt $lo -or $off -gt $hi) { continue }
		$ii = $ins[$j]
		$txt = $ii.OpCode.Name
		$oo = $ii.Operand
		if ($oo -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $oo.DeclaringType.Name + '::' + $oo.Name }
		elseif ($oo -is [Mono.Cecil.FieldReference]) { $txt += ' ' + $oo.DeclaringType.Name + '::' + $oo.Name }
		elseif ($oo -ne $null) { $txt += ' ' + $oo.ToString() }
		Write-Output ('   IL_{0:X4}: {1}' -f $off, $txt)
	}
}

Write-Output '===== 0x0850-0x09A0 ====='
Show-Range $ins 0x0850 0x09A0
Write-Output '===== 0x12A0-0x13C0 ====='
Show-Range $ins 0x12A0 0x13C0
