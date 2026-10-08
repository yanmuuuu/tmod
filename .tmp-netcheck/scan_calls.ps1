$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\开发\.tmp-netcheck\tmod_extract\WastelandSoul.dll')

function Find-Type($types, $name) {
	foreach ($t in $types) {
		if ($t.FullName -eq $name) { return $t }
		if ($t.NestedTypes.Count -gt 0) { $r = Find-Type $t.NestedTypes $name; if ($r) { return $r } }
	}
	return $null
}

# 统计每个方法里 BinaryWriter/ModPacket 的 Write / Send 调用与 BinaryReader 的 Read 调用
$interesting = @('Write', 'Read', 'Send', 'GetPacket')
foreach ($tn in @('WastelandSoul.Common.Systems.FireplaceTravelNet',
                  'WastelandSoul.Common.Systems.FireplaceTravelNet/TravelProtocol',
                  'WastelandSoul.Common.Systems.WastelandStorySystem',
                  'WastelandSoul.Common.Players.WastelandPlayer',
                  'WastelandSoul.Common.Systems.WastelandMemorySystem')) {
	$t = Find-Type $asm.MainModule.Types $tn
	if (-not $t) { Write-Output "NOT FOUND $tn"; continue }
	Write-Output ("##### {0}" -f $t.FullName)
	foreach ($m in $t.Methods) {
		if (-not $m.HasBody) { continue }
		$calls = @()
		foreach ($i in $m.Body.Instructions) {
			if ($i.OpCode.Name -notlike 'call*') { continue }
			$o = $i.Operand
			if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
			if ($interesting -contains $o.Name) {
				$calls += ("{0}::{1}" -f $o.DeclaringType.Name, $o.Name)
			}
		}
		if ($calls.Count -gt 0) {
			Write-Output ("  {0}({1}) -> {2}" -f $m.Name, (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','), ($calls -join ' | '))
		}
	}
}
