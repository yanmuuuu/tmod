# 用 tModLoader 自带的 Roslyn 直接把一个小程序编译成 exe（这台机器上没有 .NET SDK，
# 只有运行时，所以 `dotnet build` 用不了）。
#
# 用途：读原版 Terraria 的 xnb 贴图尺寸（见 Program.cs）。

$ErrorActionPreference = 'Stop'

$libs = 'E:\steam\steamapps\common\tModLoader\Libraries'
$runtime = 'E:\steam\steamapps\common\tModLoader\dotnet\shared\Microsoft.NETCore.App\8.0.0'
$srcDir = 'E:\开发\.tmp-texprobe'
$outDir = Join-Path $srcDir 'out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Add-Type -Path (Join-Path $libs 'microsoft.codeanalysis.common\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $libs 'microsoft.codeanalysis.csharp\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.CSharp.dll')

$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
  [System.IO.File]::ReadAllText((Join-Path $srcDir 'Program.cs')))

$refs = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]'

# 运行时的实现程序集直接当引用用（不追求干净，只要编得过）
foreach ($name in @('System.Runtime.dll', 'System.Console.dll', 'System.IO.FileSystem.dll',
                    'System.Private.CoreLib.dll', 'System.Linq.dll', 'netstandard.dll',
                    'System.Collections.dll', 'System.Memory.dll')) {
  $path = Join-Path $runtime $name
  if (Test-Path $path) {
    $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path))
  }
}

$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile(
  (Join-Path $libs 'FNA\1.0.0\FNA.dll')))

$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new(
  [Microsoft.CodeAnalysis.OutputKind]::ConsoleApplication)

$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create(
  'TexProbe', @($tree), $refs, $options)

$exePath = Join-Path $outDir 'TexProbe.dll'
$emit = $compilation.Emit($exePath)

if (-not $emit.Success) {
  foreach ($d in $emit.Diagnostics) { Write-Output $d.ToString() }
  throw 'compile failed'
}

Write-Output "compiled -> $exePath"

# 把 FNA 与原生依赖拷到输出目录旁边
Copy-Item (Join-Path $libs 'FNA\1.0.0\FNA.dll') $outDir -Force
Copy-Item (Join-Path $libs 'Native\Windows\*.dll') $outDir -Force
Copy-Item (Join-Path $libs 'FNA\1.0.0\FNA.pdb') $outDir -Force -ErrorAction SilentlyContinue

$runtimeConfig = @'
{
  "runtimeOptions": {
    "tfm": "net8.0",
    "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }
  }
}
'@
[System.IO.File]::WriteAllText((Join-Path $outDir 'TexProbe.runtimeconfig.json'), $runtimeConfig)

Write-Output "=== probe ==="
$ids = @('116','132','157','173','972','976','984','985','459','461','731','732','255','254',
         '443','954','682','406','510','511','512','513','523','228','569','937','926','187',
         '355','4','82','91','103','600','312','313','314','315','316','317')
& 'E:\steam\steamapps\common\tModLoader\dotnet\dotnet.exe' $exePath @ids
