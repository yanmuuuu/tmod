$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

foreach ($tn in @('Terraria.Player', 'Terraria.Item', 'Terraria.Main', 'Terraria.MessageBuffer', 'Terraria.NetMessage')) {
	$t = $modAsm.MainModule.GetType($tn)
	foreach ($m in $t.Methods) {
		if (-not $m.HasBody) { continue }
		$ins = @($m.Body.Instructions)
		for ($k = 0; $k -lt $ins.Count - 4; $k++) {
			# 精确匹配 SendData 的头部：msgType=5, remoteClient=-1, ignoreClient=-1, text=null
			if ($ins[$k].OpCode.Name -ne 'ldc.i4.5') { continue }
			if ($ins[$k + 1].OpCode.Name -ne 'ldc.i4.m1') { continue }
			if ($ins[$k + 2].OpCode.Name -ne 'ldc.i4.m1') { continue }
			if ($ins[$k + 3].OpCode.Name -ne 'ldnull') { continue }
			Write-Output ('SEND5 {0}::{1} @IL_{2:X4}' -f $t.Name, $m.Name, $ins[$k].Offset)
		}
	}
}
