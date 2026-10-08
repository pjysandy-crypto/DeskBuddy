param([switch]$Test,[switch]$Preview,[string]$OutputName='DeskBuddy.exe')
$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
if(!(Test-Path -LiteralPath $compiler)){throw 'Windows .NET Framework 4.x compiler is required.'}
if([IO.Path]::GetFileName($OutputName) -ne $OutputName){throw 'OutputName must be a file name.'}
$release=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$executable=Join-Path $release $OutputName
$font=Join-Path $PSScriptRoot 'assets\neodgm.ttf'
if(!(Test-Path -LiteralPath $font)){throw 'Pixel font missing: assets/neodgm.ttf'}
$characterResources=@()
$characterFolder=Join-Path $PSScriptRoot 'img\Character'
if(Test-Path -LiteralPath $characterFolder){$characterResources=@(Get-ChildItem -LiteralPath $characterFolder -Filter '*.png' -File | ForEach-Object { '/resource:'+$_.FullName+',DeskBuddy.Character.'+$_.BaseName })}
$companionResources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'img\Companions') -Filter '*.png' -File | ForEach-Object { '/resource:'+$_.FullName+',DeskBuddy.Companion.'+$_.BaseName })
& $compiler ("/win32icon:"+(Join-Path $PSScriptRoot "assets\deskbuddy.ico")) ("/resource:"+(Join-Path $PSScriptRoot "assets\deskbuddy.ico")+",DeskBuddy.AppIcon") /nologo /target:winexe /optimize+ /platform:anycpu /codepage:65001 ("/r:"+ (Join-Path $PSScriptRoot "lib\ComponentFactory.Krypton.Toolkit.dll")) ("/resource:"+ (Join-Path $PSScriptRoot "lib\ComponentFactory.Krypton.Toolkit.dll")+",DeskBuddy.Krypton") /r:System.dll /r:System.Core.dll /r:System.Security.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll ("/resource:"+$font+",DeskBuddy.PixelFont") @characterResources @companionResources ("/out:"+$executable) (Join-Path $PSScriptRoot 'DeskBuddy.cs') (Join-Path $PSScriptRoot 'PixelUI.cs') (Join-Path $PSScriptRoot 'AlphaWindow.cs') (Join-Path $PSScriptRoot 'Volleyball.cs') (Join-Path $PSScriptRoot 'ScheduleCore.cs') (Join-Path $PSScriptRoot 'ScheduleEditor.cs') (Join-Path $PSScriptRoot 'Castle.cs') (Join-Path $PSScriptRoot 'CharacterLibrary.cs') (Join-Path $PSScriptRoot 'Living.cs') (Join-Path $PSScriptRoot 'Companion.cs') (Join-Path $PSScriptRoot 'SizePreview.cs') (Join-Path $PSScriptRoot 'Admin.cs') (Join-Path $PSScriptRoot 'Dodge.cs') (Join-Path $PSScriptRoot 'Basketball.cs') (Join-Path $PSScriptRoot 'GameAlert.cs') (Join-Path $PSScriptRoot 'Progression.cs') (Join-Path $PSScriptRoot 'GoogleCalendar.cs') (Join-Path $PSScriptRoot 'ScheduleCalendar.cs') (Join-Path $PSScriptRoot 'FocusTimer.cs') (Join-Path $PSScriptRoot 'CompanionSprites.cs') (Join-Path $PSScriptRoot 'ScheduleRangeTests.cs') (Join-Path $PSScriptRoot 'StickyMemos.cs') (Join-Path $PSScriptRoot 'WorkDiary.cs') (Join-Path $PSScriptRoot 'MonthlyReport.cs')
if($LASTEXITCODE -ne 0){throw 'Build failed'}
if($Test){
 $process=Start-Process -FilePath $executable -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
 if($process.ExitCode -ne 0){throw 'Self-test failed'}
 Write-Host 'PASS: rewards, focus, shop, persistence, rendering, game daily limits'
}
if($Preview){
 $process=Start-Process -FilePath $executable -ArgumentList '--preview' -WindowStyle Hidden -PassThru -Wait
 if($process.ExitCode -ne 0){throw 'Preview failed'}
}
Write-Host ('Built: '+$executable)
