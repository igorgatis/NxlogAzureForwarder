# Stop on first error.
$ErrorActionPreference = "Stop"

$env:InstallPath = "${Env:ProgramFiles(x86)}\nxlog"
Write-Host "InstallPath: " $env:InstallPath

Write-Host "Installing NXLog..."
Start-Process `
   -File ($PSScriptRoot + "\nxlog-ce-2.9.1347.msi") `
   -Arg "/quiet /norestart" -Wait -PassThru | Wait-Process

Write-Host "Installing NxlogAzureForwarder..."
Copy-Item ($PSScriptRoot + "\NxlogAzureForwarder.Merged.exe") $env:InstallPath

Write-Host "Configuring NXLog..."
Get-Content ($PSScriptRoot + "\nxlog.conf.template") | ForEach-Object {
  $([System.Environment]::ExpandEnvironmentVariables($_))
} | Set-Content ($env:InstallPath + "\conf\nxlog.conf")


Write-Host "Restarting NXLog."
Restart-Service nxlog

Write-Host "Done."
