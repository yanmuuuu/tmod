$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')
Write-Output ('ASM: ' + $modAsm.MainModule.Name)

function Dump-Method($t, $m) {
	Write-Output ('### {0}::{1}  (body={2})' -f $t.FullName, $m.Name, $m.HasBody)
	if (-not $m.HasBody) { return }
	foreach ($i in $m.Body.Instructions) {
		$op = $i.OpCode.Name
		$operand = ''
		if ($null -ne $i.Operand) {
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $operand = ($o.DeclaringType.FullName + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = ($o.DeclaringType.FullName + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
			elseif ($o -is [string]) { $operand = '"' + $o + '"' }
			else { $operand = $o.ToString() }
		}
		Write-Output ('   IL_{0:X4}: {1} {2}' -f $i.Offset, $op, $operand)
	}
}

$targets = @(
	'Terraria.ModLoader.ModPlayer::SyncPlayer',
	'Terraria.ModLoader.ModPlayer::SendClientChanges',
	'Terraria.ModLoader.ModPlayer::CopyClientState',
	'Terraria.Player::AddBuff'
)

foreach ($target in $targets) {
	$parts = $target -split '::'
	$targetType = $modAsm.MainModule.GetType($parts[0])
	if ($null -eq $targetType) { Write-Output ('!! type not found: ' + $parts[0]); continue }
	foreach ($m in $targetType.Methods) {
		if ($m.Name -eq $parts[1]) { Dump-Method $targetType $m }
	}
}
