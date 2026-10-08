$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

foreach ($name in @('NetSend', 'NetReceive')) {
	foreach ($t in $modAsm.MainModule.GetTypes()) {
		foreach ($m in $t.Methods) {
			if (-not $m.HasBody) { continue }
			foreach ($i in $m.Body.Instructions) {
				$o = $i.Operand
				if ($o -is [Mono.Cecil.MethodReference] -and $o.Name -eq $name -and $o.DeclaringType.Name -match 'ModSystem|ModLoader') {
					Write-Output ('{0} <- {1}::{2} (target {3})' -f $name, $t.FullName, $m.Name, $o.DeclaringType.FullName)
				}
			}
		}
	}
}
