$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$built = Join-Path $projectRoot 'build\Upocourt.exe'
$portable = Join-Path $projectRoot 'play me\Upocourt.exe'
$installer = Join-Path $projectRoot 'play me\Install Upocourt.exe'
$zip = Join-Path $projectRoot 'play me\Upocourt-Windows.zip'
$expected = (Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash
if ((Get-FileHash -LiteralPath $portable -Algorithm SHA256).Hash -ne $expected) { throw 'Portable game is stale.' }
$gameAssembly = [Reflection.Assembly]::LoadFile($portable)
if ($null -ne $gameAssembly.GetType('Upocourt.Tests', $false)) { throw 'Test helper found in playable EXE.' }
if ($null -eq $gameAssembly.GetManifestResourceStream('Upocourt.License')) { throw 'License missing from portable game.' }
$installerAssembly = [Reflection.Assembly]::LoadFile($installer)
$payload = $installerAssembly.GetManifestResourceStream('Upocourt.Payload')
if ($null -eq $payload) { throw 'Installer payload missing.' }
$sha = [Security.Cryptography.SHA256]::Create()
try { $embedded = ([BitConverter]::ToString($sha.ComputeHash($payload))).Replace('-', '') } finally { $payload.Dispose(); $sha.Dispose() }
if ($embedded -ne $expected) { throw 'Installer contains a stale game.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $names = @($archive.Entries | ForEach-Object { $_.FullName })
    foreach ($required in @('Upocourt.exe', 'Install Upocourt.exe', 'Play.cmd', 'LICENSE.txt', 'README.md')) {
        if ($required -notin $names) { throw "ZIP is missing $required" }
    }
    if ($names.Count -ne 5) { throw 'ZIP has unexpected entries.' }
} finally { $archive.Dispose() }
Write-Host 'Portable, embedded payload, license, test exclusion and ZIP checks passed.'
Get-FileHash -LiteralPath $portable,$installer,$zip -Algorithm SHA256 | Format-Table -AutoSize
