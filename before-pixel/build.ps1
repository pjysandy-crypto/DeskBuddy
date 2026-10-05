param([switch]$Test,[switch]$Preview)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework 4.x compiler is required.' }
$release = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $release -Force | Out-Null
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll ("/out:" + (Join-Path $release 'DeskBuddy.exe')) (Join-Path $PSScriptRoot 'DeskBuddy.cs')
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
if ($Test) {
 $process = Start-Process -FilePath (Join-Path $release 'DeskBuddy.exe') -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
 if ($process.ExitCode -ne 0) { throw 'Self-test failed' }
 Write-Host 'PASS: rewards, duplicate protection, focus timer, purchase, JSON roundtrip, rendering, game cap and daily limit'
}
if ($Preview) {
 $process = Start-Process -FilePath (Join-Path $release 'DeskBuddy.exe') -ArgumentList '--preview' -WindowStyle Hidden -PassThru -Wait
 if ($process.ExitCode -ne 0) { throw 'Preview failed' }
}
Write-Host ('Built: ' + (Join-Path $release 'DeskBuddy.exe'))
