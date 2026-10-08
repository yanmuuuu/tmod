param(
	[string[]]$Targets = @(),
	[string[]]$FindCallers = @(),
	[string]$Asm = 'E:\steam\steamapps\common\tModLoader\tModLoader.dll'
)
$ErrorActionPreference = 'Stop'
$cecil = 'E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll'
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Asm)

function Dump-Method($t, $m) {
	Write-Output ('### {0}::{1}  (body={2})' -f $t.FullName, $m.Name, $m.HasBody)
	if (-not $m.HasBody) { return }
	foreach ($i in $m.Body.Instructions) {
		$op = $i.OpCode.Name
		$operand = ''
		if ($null -ne $i.Operand) {
			$o = $i.Operand
			if ($o -is [Mono.Cecil.MethodReference]) { $operand = ($o.DeclaringType.FullName + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = ($o.DeclaringType.FullName + '::' + $o.Name) }
			elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
			elseif ($o -is [string]) { $operand = '"' + $o + '"' }
			else { $operand = $o.ToString() }
		}
		Write-Output ('   IL_{0:X4}: {1} {2}' -f $i.Offset, $op, $operand)
	}
}

foreach ($target in $Targets) {
	$parts = $target -split '::'
	$type = $asm.MainModule.GetType($parts[0])
	if ($null -eq $type) { Write-Output ("!! type not found: " + $parts[0]); continue }
	foreach ($m in $type.Methods) {
		if ($m.Name -eq $parts[1]) { Dump-Method $type $m }
	}
}

if ($FindCallers.Count -gt 0) {
	foreach ($t in $asm.MainModule.GetTypes()) {
		foreach ($m in $t.Methods) {
			if (-not $m.HasBody) { continue }
			foreach ($i in $m.Body.Instructions) {
				$o = $i.Operand
				if ($o -is [Mono.Cecil.MethodReference]) {
					foreach ($f in $FindCallers) {
						if ($o.Name -eq $f) {
							Write-Output ('CALLER {0}::{1}  -> {2}::{3}' -f $t.FullName, $m.Name, $o.DeclaringType.FullName, $o.Name)
						}
					}
				}
			}
		}
	}
}
