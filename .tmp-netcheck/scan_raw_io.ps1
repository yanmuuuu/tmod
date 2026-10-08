$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil

# 扫描整个程序集：找出所有 BinaryWriter::Write / BinaryReader::Read* 的直接调用，
# 按"调用点"输出。用于确认新构建里再没有 packet.Write(x) 这种让重载决议决定宽度的写法。
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)

function Walk($types) {
	foreach ($t in $types) {
		foreach ($m in $t.Methods) {
			if (-not $m.HasBody) { continue }
			foreach ($i in $m.Body.Instructions) {
				if ($i.OpCode.Name -notlike 'call*') { continue }
				$o = $i.Operand
				if (-not ($o -is [Mono.Cecil.MethodReference])) { continue }
				$dn = $o.DeclaringType.Name
				if ($dn -notin @('BinaryWriter', 'BinaryReader')) { continue }
				$ps = ($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ','
				Write-Output ("{0}::{1}  ->  {2}::{3}({4})" -f $t.FullName, $m.Name, $dn, $o.Name, $ps)
			}
		}
		if ($t.NestedTypes.Count -gt 0) { Walk $t.NestedTypes }
	}
}
Walk $asm.MainModule.Types
