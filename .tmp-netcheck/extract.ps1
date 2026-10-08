$ErrorActionPreference = 'Stop'
$tmod = Join-Path $env:USERPROFILE 'Documents\My Games\Terraria\tModLoader\Mods\WastelandSoul.tmod'
Write-Output "tmod = $tmod  exists=$(Test-Path $tmod)"
$zip = Join-Path $env:TEMP 'ws_tmod.zip'
Copy-Item $tmod $zip -Force
$out = Join-Path $env:TEMP 'ws_tmod_x'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $out -Force
Get-ChildItem $out -Recurse -File | Select-Object FullName, Length, LastWriteTime | Format-Table -AutoSize | Out-String -Width 220
Write-Output '--- source mtimes ---'
Get-ChildItem 'E:\开发\WastelandSoul\Common\Players\WastelandPlayer.cs','E:\开发\WastelandSoul\Common\Systems\FireplaceTravelNet.cs','E:\开发\WastelandSoul\Common\Systems\WastelandStorySystem.cs' | Select-Object Name, LastWriteTime | Format-Table -AutoSize | Out-String -Width 200
