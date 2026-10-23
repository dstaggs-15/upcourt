@echo off
cd /d "%~dp0"
if exist "play me\Upocourt.exe" (
 start "" "play me\Upocourt.exe"
) else if exist "Upocourt.exe" (
 start "" "Upocourt.exe"
) else (
 echo Build the game with build.ps1 first.
 pause
)
