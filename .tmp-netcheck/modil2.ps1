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

# 打印每个 BinaryWriter::Write / BinaryReader::Read* / ModPacket::Send 的完整重载签名，
# 便于逐字节核对（Write(Int32) = 4 字节，Write(Byte) = 1 字节 ...）
foreach ($spec in $targets) {
	$parts = $spec.Split('|')
	$t = Find-Type $asm.MainModule.Types $parts[0]
	if (-not $t) { Write-Output "NOT FOUND $($parts[0])"; continue }
	foreach ($m in $t.Methods) {
		if ($parts.Count -gt 1 -and $parts[1] -and $m.Name -ne $parts[1]) { continue }
		if (-not $m.HasBody) { continue }
		Write-Output ("===== {0}::{1}" -f $t.FullName, $m.Name)
		foreach ($i in $m.Body.Instructions) {
			if ($i.OpCode.Name -notlike 'call*') { continue }
			$o = $i.Operand
			if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
			$dn = $o.DeclaringType.Name
			if ($dn -notin @('BinaryWriter', 'BinaryReader', 'ModPacket', 'Mod', 'BinaryIO')) { continue }
			$ps = ($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','
			Write-Output ("   IL_{0:X4}: {1}::{2}({3})" -f $i.Offset, $dn, $o.Name, $ps)
		}
	}
}
