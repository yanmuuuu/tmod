$ErrorActionPreference = 'Stop'
$cecil = "E:\steam\steamapps\common\tModLoader\Libraries\mono.cecil\0.11.6\lib\netstandard2.0\Mono.Cecil.dll"
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$env:DSH_WD_DIR\WeaponDisplay.dll")

function Dump-Method($t, $m) {
  Write-Output ("### {0}::{1}" -f $t.FullName, $m.Name)
  if (-not $m.HasBody) { Write-Output "   (no body)"; return }
  foreach ($i in $m.Body.Instructions) {
    $op = $i.OpCode.Name
    $operand = ""
    if ($i.Operand -ne $null) {
      $o = $i.Operand
      if ($o -is [Mono.Cecil.MethodReference]) { $operand = ($o.DeclaringType.FullName + "::" + $o.Name + "(" + (($o.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ",") + ")") }
      elseif ($o -is [Mono.Cecil.FieldReference]) { $operand = ($o.DeclaringType.FullName + "::" + $o.Name) }
      elseif ($o -is [Mono.Cecil.TypeReference]) { $operand = $o.FullName }
      elseif ($o -is [string]) { $operand = '"' + $o + '"' }
      else { $operand = $o.ToString() }
    }
    Write-Output ("   IL_{0:X4}: {1} {2}" -f $i.Offset, $op, $operand)
  }
}

$targets = @(
  "WeaponDisplay.WeaponDisplay::DrawSwoosh",
  "WeaponDisplay.WeaponDisplay::DrawSwoosh_AlphaBlend",
  "WeaponDisplay.WeaponDisplay::On_PlayerDrawLayers_DrawPlayer_27_HeldItem",
  "WeaponDisplay.WeaponDisplay::Load",
  "WeaponDisplay.SlashGlobalItem::UseItemHitbox",
  "WeaponDisplay.SlashGlobalItem::CanUseItem",
  "WeaponDisplay.SlashGlobalItem::UseStyle",
  "WeaponDisplay.SlashGlobalItem::AppliesToEntity",
  "WeaponDisplay.WeaponDisplayPlayer::ModifyDrawInfo",
  "WeaponDisplay.WeaponDisplayPlayer::CheckItemCanUse",
  "WeaponDisplay.WeaponParry::On_Player_ItemCheck_ManageRightClickFeatures_ShieldRaise"
)

function Walk($t) {
  foreach ($m in $t.Methods) {
    $key = ($t.FullName + "::" + $m.Name)
    if ($targets -contains $key) { Dump-Method $t $m }
  }
  foreach ($n in $t.NestedTypes) { Walk $n }
}
Write-Output "==================== IL ===================="
foreach ($t in $asm.MainModule.Types) { Walk $t }
