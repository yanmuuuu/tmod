$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$t = $modAsm.MainModule.GetType('Terraria.Player')
foreach ($m in $t.Methods) {
	if ($m.Name -ne 'QuickSpawnItem') { continue }
	Write-Output ('### {0}  params={1}' -f $m.FullName, (($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '))
	if (-not $m.HasBody) { continue }
	foreach ($i in $m.Body.Instructions) {
		$txt = $i.OpCode.Name
		$o = $i.Operand
		if ($o -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
		elseif ($o -is [Mono.Cecil.FieldReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
		elseif ($o -ne $null) { $txt += ' ' + $o.ToString() }
		Write-Output ('   IL_{0:X4}: {1}' -f $i.Offset, $txt)
	}
}
