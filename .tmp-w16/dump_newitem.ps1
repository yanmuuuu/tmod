$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

function Dump-Filtered($typeName, $methodName, $pattern) {
	$t = $modAsm.MainModule.GetType($typeName)
	foreach ($m in $t.Methods) {
		if ($m.Name -ne $methodName) { continue }
		Write-Output ('### {0}::{1} (body={2})' -f $t.FullName, $m.Name, $m.HasBody)
		if (-not $m.HasBody) { return }
		$idx = 0
		foreach ($i in $m.Body.Instructions) {
			$txt = $i.OpCode.Name
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -ne $null) { $txt += ' ' + $o.ToString() }
			if ($txt -match $pattern) { Write-Output ('   IL_{0:X4}: {1}' -f $i.Offset, $txt) }
			$idx++
		}
	}
}

Write-Output '===== Item::NewItem ====='
Dump-Filtered 'Terraria.Item' 'NewItem' 'netMode|SendData|dedServ|myPlayer'
Write-Output '===== Player::QuickSpawnItem ====='
Dump-Filtered 'Terraria.Player' 'QuickSpawnItem' 'netMode|SendData|Item::NewItem|noBroadcast|QuickSpawn'
