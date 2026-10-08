$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil

# 定宽字节数表：含我们自己的 NetWriter / NetReader（方法名带宽度后缀，改宽度就改名字）
$fixedWidths = @{
	'WriteByte'    = 1
	'WriteBool'    = 1
	'WriteInt32'   = 4
	'WriteSingle'  = 4
	'WriteInt16'   = 2
	'WriteUInt16'  = 2
	'ReadByte'     = 1
	'ReadSByte'    = 1
	'ReadBoolean'  = 1
	'ReadInt16'    = 2
	'ReadUInt16'   = 2
	'ReadInt32'    = 4
	'ReadUInt32'   = 4
	'ReadInt64'    = 8
	'ReadUInt64'   = 8
	'ReadSingle'   = 4
	'ReadDouble'   = 8
	'Read7BitEncodedInt' = 1
}
$bwWidths = @{ 'Byte' = 1; 'SByte' = 1; 'Boolean' = 1; 'Int16' = 2; 'UInt16' = 2; 'Int32' = 4; 'UInt32' = 4; 'Int64' = 8; 'UInt64' = 8; 'Single' = 4; 'Double' = 8 }

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
		if ($bwWidths.ContainsKey($pt)) { return $bwWidths[$pt] }
		return -1   # String / byte[] 等变长
	}
	if ($dn -eq 'ModPacket' -and $mref.Name -eq 'Send') { return 0 }
	if ($fixedWidths.ContainsKey($mref.Name)) { return $fixedWidths[$mref.Name] }
	return -2
}

function Measure-Method($type, $methodName) {
	$m = $type.Methods | Where-Object { $_.Name -eq $methodName } | Select-Object -First 1
	if (-not $m) { return [pscustomobject]@{ Fixed = -99; Var = @('MISSING') } }
	if (-not $m.HasBody) { return [pscustomobject]@{ Fixed = -99; Var = @('NOBODY') } }
	$fixed = 0
	$var = @()
	foreach ($i in $m.Body.Instructions) {
		if ($i.OpCode.Name -notlike 'call*') { continue }
		$o = $i.Operand
		if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
		$dn = $o.DeclaringType.Name
		if ($dn -notin @('BinaryWriter', 'BinaryReader', 'ModPacket', 'NetWriter', 'NetReader')) { continue }
		$w = Get-Width $o
		if ($w -ge 0) { $fixed += $w }
		else { $var += ("$dn::$($o.Name)") }
	}
	return [pscustomobject]@{ Fixed = $fixed; Var = $var }
}

function Show($label, $typeName, $methodName) {
	$t = Find-Type $script:asm.MainModule.Types $typeName
	if (-not $t) { Write-Output ("  {0,-34} <类型不存在 {1}>" -f $label, $typeName); return }
	$r = Measure-Method $t $methodName
	$varTxt = if ($r.Var.Count -gt 0) { ' + 变长[' + ($r.Var -join ',') + ']' } else { '' }
	Write-Output ("  {0,-34} 定宽 {1,3} 字节{2}" -f $label, $r.Fixed, $varTxt)
}

$script:asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)
Write-Output "########## $asmPath"
Write-Output '1) 剧情同步（服务端 -> 客户端）'
Show '  写 WriteStoryPayload' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'WriteStoryPayload'
Show '  读 ReadStoryPayload' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'ReadStoryPayload'
Write-Output '2) 壁炉进出（客户端 -> 服务端）'
Show '  写 TravelProtocol.Send' 'WastelandSoul.Common.Systems.FireplaceTravelNet/TravelProtocol' 'Send'
Show '  读 ReceiveInnerPacket' 'WastelandSoul.Common.Systems.FireplaceTravelNet' 'ReceiveInnerPacket'
Show '  读 ReceiveOnServer' 'WastelandSoul.Common.Systems.FireplaceTravelNet' 'ReceiveOnServer'
Write-Output '2b) 壁炉状态（服务端 -> 客户端）'
Show '  写 SendToClient' 'WastelandSoul.Common.Systems.FireplaceTravelNet' 'SendToClient'
Show '  写 SyncMassFormed' 'WastelandSoul.Common.Systems.FireplaceTravelNet' 'SyncMassFormed'
Show '  读 ReceiveOnClient' 'WastelandSoul.Common.Systems.FireplaceTravelNet' 'ReceiveOnClient'
Write-Output '3) 剧情请求（客户端 -> 服务端）'
Show '  写 RequestDataTerminalRead' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'RequestDataTerminalRead'
Show '  写 RequestEndingChoice' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'RequestEndingChoice'
Show '  写 RequestFireplaceSeen' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'RequestFireplaceSeen'
Show '  写 RequestCompanionSummon' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'RequestCompanionSummon'
Show '  写 RequestFragmentDelivery' 'WastelandSoul.Common.Systems.WastelandStorySystem' 'RequestFragmentDelivery'
Show '  读 ReceiveRequest' 'WastelandSoul.Common.Systems.WastelandNet' 'ReceiveRequest'
Write-Output '4) 个人进度（客户端 -> 服务端）'
Show '  写 SyncPlayer' 'WastelandSoul.Common.Players.WastelandPlayer' 'SyncPlayer'
Show '  读 ReceiveProgress' 'WastelandSoul.Common.Players.WastelandPlayer' 'ReceiveProgress'
Write-Output '5) 碎片交付回执（服务端 -> 客户端）'
Show '  写 SendDeliveryAck' 'WastelandSoul.Common.Systems.WastelandMemorySystem' 'SendDeliveryAck'
Show '  读 ReceiveDeliveryAck' 'WastelandSoul.Common.Systems.WastelandMemorySystem' 'ReceiveDeliveryAck'
