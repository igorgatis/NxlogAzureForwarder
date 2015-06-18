@echo off
PowerShell.exe -NoLogo -ExecutionPolicy UnRestricted -File "%~dp0\setup.ps1" 1> "%~dp0\NAFInstall.log" 2>&1
set level=%ERRORLEVEL%
type "%~dp0\NAFInstall.log"
exit /b %level%
