$ErrorActionPreference = 'Stop'
$cecilPath = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecilPath
$modAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('E:\steam\steamapps\common\tModLoader\tModLoader.dll')

$out = New-Object System.Collections.Generic.List[string]

foreach ($t in $modAsm.MainModule.GetTypes()) {
	foreach ($m in $t.Methods) {
		if (-not $m.HasBody) { continue }
		$ins = @($m.Body.Instructions)
		for ($k = 0; $k -lt $ins.Count; $k++) {
			$i = $ins[$k]
			$is55 = ($i.OpCode.Name -eq 'ldc.i4.s' -and "$($i.Operand)" -eq '55') -or ($i.OpCode.Name -eq 'ldc.i4' -and "$($i.Operand)" -eq '55')
			if (-not $is55) { continue }
			$hasSend = $false
			for ($j = $k; $j -lt [Math]::Min($k + 16, $ins.Count); $j++) {
				$o = $ins[$j].Operand
				if ($o -is [Mono.Cecil.MethodReference] -and $o.Name -eq 'SendData') { $hasSend = $true; break }
			}
			if ($hasSend) { $out.Add(('SEND55 {0}::{1} @IL_{2:X4}' -f $t.FullName, $m.Name, $i.Offset)) }
		}
	}
}

Write-Output ('total lines: ' + $out.Count)
foreach ($l in $out) { Write-Output $l }
