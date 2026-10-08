$ErrorActionPreference = 'Continue'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil

$fixedWidths = @{
	'WriteByte' = 1; 'WriteBool' = 1; 'WriteInt32' = 4; 'WriteSingle' = 4
	'ReadByte' = 1; 'ReadBoolean' = 1; 'ReadInt32' = 4; 'ReadSingle' = 4; 'Read7BitEncodedInt' = 1
}
$bwWidths = @{ 'Byte' = 1; 'Boolean' = 1; 'Int16' = 2; 'UInt16' = 2; 'Int32' = 4; 'UInt32' = 4; 'Int64' = 8; 'UInt64' = 8; 'Single' = 4; 'Double' = 8 }

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
	if (-not $type) { return $null }
	$m = $type.Methods | Where-Object { $_.Name -eq $method } | Select-Object -First 1
	if (-not $m -or -not $m.HasBody) { return $null }
	$sum = 0; $var = 0
	foreach ($i in $m.Body.Instructions) {
		if ($i.OpCode.Name -notlike 'call*') { continue }
		$o = $i.Operand
		if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
		$dn = $o.DeclaringType.Name
		if ($dn -notin @('BinaryWriter', 'BinaryReader', 'ModPacket', 'NetWriter', 'NetReader')) { continue }
		$w = Width $o
		if ($w -ge 0) { $sum += $w } elseif ($w -eq -1) { $var++ }
	}
	return [pscustomobject]@{ Fixed = $sum; Var = $var }
}
function Fmt($m, $extra = 0) {
	if (-not $m) { return 'n/a' }
	$t = "$($m.Fixed + $extra)"
	if ($m.Var -gt 0) { $t += "+$($m.Var)v" }
	return $t
}

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)
$ss = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.WastelandStorySystem'
$wn = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.WastelandNet'
$ft = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.FireplaceTravelNet'
$tp = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.FireplaceTravelNet/TravelProtocol'
$wp = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Players.WastelandPlayer'
$ms = Find-Type $asm.MainModule.Types 'WastelandSoul.Common.Systems.WastelandMemorySystem'

# 收包侧的"分发字节"由 ReceivePacket/WastelandNet.ReceivePacket 读掉（+1），
# 每条记录的读侧 = 分发(1) + 内层字段
Write-Output "===== $asmPath"
Write-Output ('{0,-30} {1,-16} {2}' -f '消息', '写(字节)', '读(字节)')
function Row($name, $w, $r) { Write-Output ('{0,-30} {1,-16} {2}' -f $name, $w, $r) }

# kind1 剧情同步（服务端->客户端）
$w = Bytes $ss 'WriteStoryPayload'; if (-not $w) { $w = Bytes $ss 'WriteStory' }
$r = Bytes $ss 'ReadStoryPayload'; if (-not $r) { $r = Bytes $ss 'ReadStory' }
Row '1 StorySync' (Fmt $w 1) (Fmt $r 1)

# kind2 请求（客户端->服务端）：三种 inner
$pw = Bytes $tp 'Send'          # 含 kind + inner (+ 可选 int32)
$pr = Bytes $ft 'ReceiveInnerPacket'   # 分发后读 inner
$psr = Bytes $ft 'ReceiveOnServer'     # 0/1 分支：无；2 分支：ReadInt32
Row '2 Travel inner0 (enter)' (Fmt $pw 1) (Fmt $pr (1 + 0 + 0))
Row '2 Travel inner1 (exit)'  (Fmt $pw 1) (Fmt $pr (1 + 0 + 0))
Row '2 Travel inner2 (reform)' (Fmt $pw 1) (Fmt $pr (1 + 0 + 0))

# kind2 状态（服务端->客户端）
$sw = Bytes $ft 'SendToClient'
$sr = Bytes $ft 'ReceiveOnClient'
Row '2 Travel state 3/4' (Fmt $sw 1) (Fmt $sr 1)

# kind3 请求
$rw = Bytes $ss 'RequestEndingChoice'
$rr = Bytes $wn 'ReceiveRequest'
Row '3 Request.action(1..5)' '(见下)' (Fmt $rr 1)
Row '   3a DataTerminalRead' '1+1+0' (Fmt $rr 0)
Row '   3b EndingChoice' '1+1+4' (Fmt $rr 0)
Row '   3c FireplaceSeen' '1+1+0' (Fmt $rr 0)
Row '   3d CompanionSummon' '1+1+8' (Fmt $rr 0)
Row '   3e DeliverFragment' '1+1+2' (Fmt $rr 0)

# kind4 个人进度
Row '4 PlayerProgress' (Fmt (Bytes $wp 'SyncPlayer') 0) (Fmt (Bytes $wp 'ReceiveProgress') 1)

# kind5 回执
Row '5 FragmentAck' (Fmt (Bytes $ms 'SendDeliveryAck') 0) (Fmt (Bytes $ms 'ReceiveDeliveryAck') 1)
