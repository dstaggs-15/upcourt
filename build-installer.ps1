$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$game = Join-Path $projectRoot 'build\Upocourt.exe'
if (!(Test-Path -LiteralPath $game)) { throw 'Run build.ps1 first.' }
$release = Join-Path $projectRoot 'play me'
New-Item -ItemType Directory -Force -Path $release | Out-Null
Copy-Item -LiteralPath $game -Destination (Join-Path $release 'Upocourt.exe') -Force
& $compiler /nologo /optimize+ /target:winexe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:"$release\Install Upocourt.exe" "/resource:$game,Upocourt.Payload" "$projectRoot\installer\Installer.cs" "$projectRoot\src\PixelArt.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
$probe = Join-Path $env:TEMP ('Upocourt-install-test-' + [Guid]::NewGuid().ToString('N'))
& "$release\Install Upocourt.exe" --install-dir $probe
if ($LASTEXITCODE -ne 0) { throw 'Fresh installation failed.' }
$expected = (Get-FileHash -LiteralPath $game -Algorithm SHA256).Hash
$installed = (Get-FileHash -LiteralPath (Join-Path $probe 'Upocourt.exe') -Algorithm SHA256).Hash
if ($expected -ne $installed) { throw 'Installed game differs from built game.' }
& "$release\Install Upocourt.exe" --install-dir $probe
if ($LASTEXITCODE -ne 0) { throw 'Reinstallation failed.' }
$installed = (Get-FileHash -LiteralPath (Join-Path $probe 'Upocourt.exe') -Algorithm SHA256).Hash
if ($expected -ne $installed) { throw 'Reinstalled game differs from built game.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'Play.cmd') -Destination (Join-Path $release 'Play.cmd') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE.txt') -Destination (Join-Path $release 'LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $release 'README.md') -Force
Compress-Archive -Path "$release\Upocourt.exe","$release\Install Upocourt.exe","$release\Play.cmd","$release\LICENSE.txt","$release\README.md" -DestinationPath "$release\Upocourt-Windows.zip" -Force
Write-Host 'Fresh install, reinstall and SHA256 payload checks passed.'
