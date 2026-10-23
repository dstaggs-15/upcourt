@echo off
cd /d "%~dp0"
if exist "play me\Upcourt.exe" (
 start "" "play me\Upcourt.exe"
) else if exist "Upcourt.exe" (
 start "" "Upcourt.exe"
) else (
 echo Build the game with build.ps1 first.
 pause
)
