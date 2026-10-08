$ErrorActionPreference = 'Continue'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil

$fixedWidths = @{
	'WriteByte' = 1; 'WriteBool' = 1; 'WriteInt32' = 4; 'WriteSingle' = 4
	'ReadByte' = 1; 'ReadBoolean' = 1; 'ReadInt32' = 4; 'ReadSingle' = 4; 'Read7BitEncodedInt' = 1
}
$bwWidths = @{ 'Byte' = 1; 'SByte' = 1; 'Boolean' = 1; 'Int16' = 2; 'UInt16' = 2; 'Int32' = 4; 'UInt32' = 4; 'Int64' = 8; 'UInt64' = 8; 'Single' = 4; 'Double' = 8 }

function Find-Type($types, $name) {
	foreach ($t in $types) {
		if ($t.FullName -eq $name) { return $t }
		if ($t.NestedTypes.Count -gt 0) { $r = Find-Type $t.NestedTypes $name; if ($r) { return $r } }
	}
	return $null
}

function Width($mref) {
	$dn = $mref.DeclaringType.Name
	if ($dn -eq 'BinaryWriter' -and $mref.Name -eq 'Write') {
		$pt = $mref.Parameters[0].ParameterType.Name
		if ($bwWidths.ContainsKey($pt)) { return $bwWidths[$pt] }
		return -1
	}
	if ($dn -eq 'ModPacket' -and $mref.Name -eq 'Send') { return 0 }
	if ($fixedWidths.ContainsKey($mref.Name)) { return $fixedWidths[$mref.Name] }
	return -2
}

function Bytes($type, $method) {
	if (-not $type) { return 'n/a' }
	$m = $type.Methods | Where-Object { $_.Name -eq $method } | Select-Object -First 1
	if (-not $m -or -not $m.HasBody) { return 'MISSING' }
	$sum = 0
	$var = 0
	foreach ($i in $m.Body.Instructions) {
		if ($i.OpCode.Name -notlike 'call*') { continue }
		$o = $i.Operand
		if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
		$dn = $o.DeclaringType.Name
		if ($dn -notin @('BinaryWriter', 'BinaryReader', 'ModPacket', 'NetWriter', 'NetReader')) { continue }
		$w = Width $o
		if ($w -ge 0) { $sum += $w } elseif ($w -eq -1) { $var++ }
	}
	if ($var -gt 0) { return "$sum+${var}v" }
	return "$sum"
}

foreach ($dll in (Get-ChildItem 'E:\开发\.tmp-netcheck\hist' -Filter '*.dll' | Sort-Object Name)) {
	try {
		$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll.FullName)
	} catch { Write-Output "$($dll.Name) LOAD-ERR"; continue }
	$wp = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Players.WastelandPlayer'
	$ft = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.FireplaceTravelNet'
	$tp = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.FireplaceTravelNet/TravelProtocol'
	$ss = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.WastelandStorySystem'
	# 4 号包：写 SyncPlayer / 读 ReceiveProgress
	$w4 = Bytes $wp 'SyncPlayer'
	$r4 = Bytes $wp 'ReceiveProgress'
	# 2 号包（请求）：写 TravelProtocol.Send / 读 ReceiveInnerPacket + ReceiveOnServer
	$w2 = Bytes $tp 'Send'
	if ($w2 -eq 'n/a') { $w2 = Bytes $ft 'SendInnerRequest' }
	$r2 = Bytes $ft 'ReceiveInnerPacket'
	# 1 号包：写 WriteStory / 读 ReadStory
	$w1 = Bytes $ss 'WriteStory'
	if ($w1 -eq 'MISSING' -or $w1 -eq 'n/a') { $w1 = Bytes $ss 'WriteStoryPayload' }
	$r1 = Bytes $ss 'ReadStory'
	if ($r1 -eq 'MISSING' -or $r1 -eq 'n/a') { $r1 = Bytes $ss 'ReadStoryPayload' }
	Write-Output ("{0,-34} kind4 写={1,-6} 读={2,-6} | kind2 写={3,-6} 读={4,-6} | kind1 写={5,-6} 读={6}" -f `
		$dll.BaseName, $w4, $r4, $w2, $r2, $w1, $r1)
}
