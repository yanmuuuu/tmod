$ErrorActionPreference = 'Continue'
$tml = 'E:\steam\steamapps\common\tModLoader'
$libs = Get-ChildItem "$tml\Libraries" -Recurse -Filter '*.dll' | Select-Object -ExpandProperty FullName

$handler = [System.ResolveEventHandler] {
	param($s, $e)
	$simple = ($e.Name -split ',')[0]
	$found = $libs | Where-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) -eq $simple } | Select-Object -First 1
	if ($found) { return [System.Reflection.Assembly]::LoadFrom($found) }
	$cand = Join-Path $tml "$simple.dll"
	if (Test-Path $cand) { return [System.Reflection.Assembly]::LoadFrom($cand) }
	return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($handler)

$asm = [System.Reflection.Assembly]::LoadFrom("$tml\tModLoader.dll")
$bio = $asm.GetType('Terraria.ModLoader.IO.BinaryIO', $false)
Write-Output "bio = $bio"
if (-not $bio) { exit 1 }
$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Static'
$safeRead = $bio.GetMethods($flags) | Where-Object { $_.Name -eq 'SafeRead' }
foreach ($m in $safeRead) {
	Write-Output "SafeRead: $($m.ToString())"
	$m.GetParameters() | ForEach-Object { Write-Output "    param: $($_.ParameterType.FullName) $($_.Name)" }
}

function Invoke-SafeRead([byte[]]$bytes, [int]$offset, [int]$handlerReads, [string]$label) {
	$ms = New-Object System.IO.MemoryStream(, $bytes)
	$br = New-Object System.IO.BinaryReader($ms)
	try {
		for ($i = 0; $i -lt $handlerReads; $i++) { [void]$br.ReadByte() }
		$m = $safeRead | Select-Object -First 1
		try {
			[void]$m.Invoke($null, @($br, $offset))
			Write-Output "$label : OK, pos=$($br.BaseStream.Position)/$($ms.Length)"
		}
		catch {
			$inner = $_.Exception.InnerException
			if (-not $inner) { $inner = $_.Exception }
			Write-Output "$label : $($inner.GetType().Name): $($inner.Message)"
		}
	}
	finally { $br.Dispose(); $ms.Dispose() }
}

# ---- 复刻真实场景：包体首字节是 7 位编码的“包体总长”，随后是 mod 名的原始字节 ----
# 例：长度字段 0x0C(12) + 12 字节 mod 名 => offset 通常 = 1 + 12 = 13
$name = [System.Text.Encoding]::UTF8.GetBytes('WastelandSoul')
$payloadLen = 1 + $name.Length + 5   # 长度字段(1) + 名字 + 5 字节真实负载
$bytes = New-Object System.Collections.Generic.List[byte]
$bytes.Add([byte]$payloadLen)
$bytes.AddRange($name)
$bytes.AddRange([byte[]]@(0x02, 0x03, 0x00, 0x00, 0x00))
$arr = $bytes.ToArray()
Write-Output "payload bytes = $($arr.Length) ($($arr | ForEach-Object { $_.ToString('X2') }) -join ' ')"

Invoke-SafeRead $arr $arr.Length 0 'caseA offset=total, reads=0'
Invoke-SafeRead $arr $arr.Length 2 'caseB offset=total, reads=2'
Invoke-SafeRead $arr $arr.Length 1 'caseC offset=total, reads=1'
Invoke-SafeRead $arr ($arr.Length - 1) 2 'caseD offset=size-1, reads=2'
