$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework 4.8 C# compiler is required.' }
$output = Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | Select-Object -ExpandProperty FullName
$gameSources = $sources | Where-Object { !(Split-Path -Leaf $_).EndsWith('Tests.cs') }
$recordings = @('stadium', 'cheer', 'applause', 'groan') | ForEach-Object { '/resource:' + (Join-Path $projectRoot ('audio\' + $_ + '.wav')) + ',Upcourt.Audio.' + $_ + '.wav' }
& $compiler /nologo /optimize+ /target:winexe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll "/resource:$projectRoot\LICENSE.txt,Upcourt.License" /out:"$output\Upcourt.exe" $recordings $gameSources
if ($LASTEXITCODE -ne 0) { throw 'Game build failed.' }
& $compiler /nologo /optimize+ /define:UPCOURT_TESTS /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll "/resource:$projectRoot\LICENSE.txt,Upcourt.License" /out:"$output\Upcourt.Tests.exe" $recordings $sources
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& "$output\Upcourt.Tests.exe" --test
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
Write-Host "Built $output\Upcourt.exe"
