@echo off
REM Needed redirect to powershell command line because VS checks for existence of task's commandLine file.
set LOGDIR=C:\Logs\NxlogAzureForwarder
mkdir %LOGDIR%
PowerShell.exe -ExecutionPolicy UnRestricted -File "%~dp0\install-plugin.ps1" > "%LOGDIR%\install-log.txt" 2>&1
exit /b 0
