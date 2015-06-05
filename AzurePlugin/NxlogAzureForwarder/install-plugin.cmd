@echo off
REM Needed redirect to powershell command line because VS checks for existence of task's commandLine file.
set ROOTDIR=%~dp0
set LOGFILE=%ROOTDIR%\install-log.txt
PowerShell.exe -ExecutionPolicy UnRestricted -File %ROOTDIR%\install-plugin.ps1 > %LOGFILE% 2>&1
exit /b 0