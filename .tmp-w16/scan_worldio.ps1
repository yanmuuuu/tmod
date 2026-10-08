$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

foreach ($name in @('SendModData', 'ReceiveModData')) {
	foreach ($t in $modAsm.MainModule.GetTypes()) {
		foreach ($m in $t.Methods) {
			if (-not $m.HasBody) { continue }
			foreach ($i in $m.Body.Instructions) {
				$o = $i.Operand
				if ($o -is [Mono.Cecil.MethodReference] -and $o.Name -eq $name) {
					Write-Output ('{0} <- {1}::{2}' -f $name, $t.FullName, $m.Name)
				}
			}
		}
	}
}

# 把 WorldIO::SendModData / ReceiveModData 的 netMode 分支打出来
foreach ($name in @('SendModData', 'ReceiveModData')) {
	$t = $modAsm.MainModule.GetType('Terraria.ModLoader.IO.WorldIO')
	foreach ($m in $t.Methods) {
		if ($m.Name -ne $name) { continue }
		Write-Output ('### {0}' -f $m.FullName)
		foreach ($i in $m.Body.Instructions) {
			$txt = $i.OpCode.Name
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $txt += ' ' + $o.DeclaringType.Name + '::' + $o.Name }
			elseif ($o -ne $null) { $txt += ' ' + $o.ToString() }
			Write-Output ('   IL_{0:X4}: {1}' -f $i.Offset, $txt)
		}
	}
}
