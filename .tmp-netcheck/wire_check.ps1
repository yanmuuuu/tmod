$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil

# 每条记录的字节宽度：按 BinaryWriter/BinaryReader 的重载名解析
$widths = @{
	'Write'          = @{ 'Byte' = 1; 'SByte' = 1; 'Boolean' = 1; 'Int16' = 2; 'UInt16' = 2; 'Int32' = 4; 'UInt32' = 4; 'Int64' = 8; 'UInt64' = 8; 'Single' = 4; 'Double' = 8; 'String' = -1; 'Char' = -1; 'Byte[]' = -1 }
	'ReadByte'       = 1
	'ReadSByte'      = 1
	'ReadBoolean'    = 1
	'ReadInt16'      = 2
	'ReadUInt16'     = 2
	'ReadInt32'      = 4
	'ReadUInt32'     = 4
	'ReadInt64'      = 8
	'ReadUInt64'     = 8
	'ReadSingle'     = 4
	'ReadDouble'     = 8
	'Write7BitEncodedInt' = 1
	'Read7BitEncodedInt'  = 1
}

function Find-Type($types, $name) {
	foreach ($t in $types) {
		if ($t.FullName -eq $name) { return $t }
		if ($t.NestedTypes.Count -gt 0) { $r = Find-Type $t.NestedTypes $name; if ($r) { return $r } }
	}
	return $null
}

function Get-Width($mref) {
	$dn = $mref.DeclaringType.Name
	if ($dn -eq 'BinaryWriter' -and $mref.Name -eq 'Write') {
		$pt = $mref.Parameters[0].ParameterType.Name
		if ($widths['Write'].ContainsKey($pt)) { return $widths['Write'][$pt] }
		return -2
	}
	if ($widths.ContainsKey($mref.Name) -and $widths[$mref.Name] -is [int]) { return [int]$widths[$mref.Name] }
	return -2
}

# 解析一个方法内的 固定宽度字节数（String / byte[] 记为 -1，表示"变长，需人工"）
function Measure-Method($type, $methodName) {
	$m = $type.Methods | Where-Object { $_.Name -eq $methodName } | Select-Object -First 1
	if (-not $m -or -not $m.HasBody) { return $null }
	$fixed = 0
	$unk = @()
	foreach ($i in $m.Body.Instructions) {
		if ($i.OpCode.Name -notlike 'call*') { continue }
		$o = $i.Operand
		if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
		$dn = $o.DeclaringType.Name
		if ($dn -notin @('BinaryWriter', 'BinaryReader', 'ModPacket')) { continue }
		$w = Get-Width $o
		if ($w -ge 0) { $fixed += $w }
		else { $unk += ("$dn::$($o.Name)(" + (($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")") }
	}
	return [pscustomobject]@{ Fixed = $fixed; Unknown = $unk; Signature = (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') }
}

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)
$checks = @(
	@{ Msg = '1 StorySync';                    Send = @('WastelandSoul.Common.Systems.WastelandStorySystem', 'Sync'); Recv = @('WastelandSoul.Common.Systems.WastelandStorySystem', 'ReceiveStorySync') },
	@{ Msg = '3 Request.DataTerminalRead(1)';  Send = @('WastelandSoul.Common.Systems.WastelandStorySystem', 'RequestDataTerminalRead'); Recv = @('WastelandSoul.Common.Systems.WastelandNet', 'ReceiveRequest') },
	@{ Msg = '4 PlayerProgress';               Send = @('WastelandSoul.Common.Players.WastelandPlayer', 'SyncPlayer'); Recv = @('WastelandSoul.Common.Players.WastelandPlayer', 'ReceiveProgress') },
	@{ Msg = '5 FragmentAck';                  Send = @('WastelandSoul.Common.Systems.WastelandMemorySystem', 'SendDeliveryAck'); Recv = @('WastelandSoul.Common.Systems.WastelandMemorySystem', 'ReceiveDeliveryAck') },
	@{ Msg = '2 Travel request inner0/1/2';    Send = @('WastelandSoul.Common.Systems.FireplaceTravelNet/TravelProtocol', 'Send'); Recv = @('WastelandSoul.Common.Systems.FireplaceTravelNet', 'ReceiveInnerPacket') },
	@{ Msg = '2 Travel state 3/4';             Send = @('WastelandSoul.Common.Systems.FireplaceTravelNet', 'SendToClient'); Recv = @('WastelandSoul.Common.Systems.FireplaceTravelNet', 'ReceiveOnClient') }
)

Write-Output "########## $asmPath"
foreach ($c in $checks) {
	$st = Find-Type $asm.MainModule.Types $c.Send[0]
	$rt = Find-Type $asm.MainModule.Types $c.Recv[0]
	$sw = if ($st) { Measure-Method $st $c.Send[1] } else { $null }
	$rw = if ($rt) { Measure-Method $rt $c.Recv[1] } else { $null }
	$sTxt = if ($sw) { "$($sw.Fixed) (+变长: $($sw.Unknown.Count))" } else { 'n/a' }
	$rTxt = if ($rw) { "$($rw.Fixed) (+变长: $($rw.Unknown.Count))" } else { 'n/a' }
	Write-Output ("{0,-28} 写={1,-22} 读={2}" -f $c.Msg, $sTxt, $rTxt)
}
