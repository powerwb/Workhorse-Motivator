$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
if (-not (Test-Path "$fw\csc.exe")) { $fw = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319" }
$wpf = "$fw\WPF"
$out = Join-Path $root '牛马鼓励器.exe'

$refs = @(
  "$wpf\PresentationFramework.dll", "$wpf\PresentationCore.dll", "$wpf\WindowsBase.dll",
  "$fw\System.Xaml.dll", "$fw\System.dll", "$fw\System.Core.dll",
  "$fw\System.Drawing.dll", "$fw\System.Windows.Forms.dll"
) | ForEach-Object { "/r:$_" }

$srcs = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName }

$res = @("/resource:$root\src\app.ico,NiuMa.app.ico")
if (Test-Path "$root\src\anim.bin") { $res += "/resource:$root\src\anim.bin,NiuMa.anim.bin" }

& "$fw\csc.exe" /nologo /target:winexe /optimize+ /codepage:65001 /platform:anycpu `
  "/out:$out" "/win32icon:$root\src\app.ico" "/win32manifest:$root\src\app.manifest" `
  @res @refs @srcs
if ($LASTEXITCODE -ne 0) { throw "编译失败" }
Write-Host "已生成 $out"
