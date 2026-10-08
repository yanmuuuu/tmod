$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$mb = $modAsm.MainModule.GetType('Terraria.MessageBuffer')
$m = $mb.Methods | Where-Object { $_.Name -eq 'GetData' }
$ins = @($m.Body.Instructions)
Write-Output ('GetData instructions: ' + $ins.Count)

for ($k = 0; $k -lt $ins.Count; $k++) {
	$o = $ins[$k].Operand
	$hit = $false
	if ($o -is [Mono.Cecil.FieldReference] -and $o.DeclaringType.Name -eq 'Player' -and $o.Name -eq 'inventory') { $hit = $true }
	if (-not $hit) { continue }
	Write-Output ('--- inventory ref @IL_{0:X4}' -f $ins[$k].Offset)
	$from = [Math]::Max(0, $k - 30)
	$to = [Math]::Min($ins.Count - 1, $k + 4)
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
