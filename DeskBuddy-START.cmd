@echo off
cd /d "%~dp0"
if not exist "dist\DeskBuddy.exe" (
 echo DeskBuddy.exe was not found. Please extract the entire ZIP first.
 pause
 exit /b 1
)
start "" "dist\DeskBuddy.exe"