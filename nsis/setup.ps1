# Stop on first error.
$ErrorActionPreference = "Stop"

Function InstallNxlogAzureForwarder
{
    Write-Host "* Removing NxlogAzureForwarder service..."
    try {
        Start-Process -File "sc" `
           -Arg "delete NxlogAzureForwarder" `
           -NoNewWindow -Wait | Wait-Process
    } catch {}

    Write-Host "* Installing NxlogAzureForwarder service..."
    $DotNetPath = $([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory())
    Start-Process `
       -File "$DotNetPath\InstallUtil.exe" `
       -Arg "/install `"$PSScriptRoot\NxlogAzureForwarder.exe`"" `
       -NoNewWindow -Wait | Wait-Process

    Write-Host "* Configuring NxlogAzureForwarder service..."
    Start-Process -File "sc" `
       -Arg "failure NxlogAzureForwarder reset= 0 actions= restart/10000" `
       -NoNewWindow -Wait | Wait-Process
}

Function InstallNxlog
{
    # WARNING: must not uninstall as Nxlog might have queued items.
    Write-Host "* Installing nxlog..."
    Start-Process -File "msiexec" `
       -Arg "/i `"$PSScriptRoot\nxlog-ce-2.9.1347.msi`" /quiet /norestart" `
       -NoNewWindow -Wait | Wait-Process

    Write-Host "* Configuring nxlog service..."
    Start-Process -File "sc" `
       -Arg "failure nxlog reset= 0 actions= restart/10000" `
       -NoNewWindow -Wait | Wait-Process

    Write-Host "* Copying nxlog.conf..."
    try { Stop-Service nxlog } catch {}
    Copy-Item "$PSScriptRoot\nxlog.conf" "$PSScriptRoot\..\nxlog\conf" -Force
}

Function CleanUp
{
    Write-Host "* Cleaning temp files..."
    Remove-Item "$PSScriptRoot\nxlog.conf"
    Remove-Item "$PSScriptRoot\nxlog-ce-2.9.1347.msi"
    Remove-Item "$PSScriptRoot\*.InstallLog"
}

try
{
    # Stopping services.
    Write-Host "* Stopping nxlog service..."
    try { Stop-Service nxlog } catch {}
    Write-Host "* Stopping NxlogAzureForwarder service..."
    try { Stop-Service NxlogAzureForwarder } catch {}

    InstallNxlogAzureForwarder
    InstallNxlog

    # Restart services.
    Write-Host "* Restart NxlogAzureForwarder service..."
    Restart-Service NxlogAzureForwarder
    Write-Host "* Restart nxlog service..."
    Restart-Service nxlog

    CleanUp

    # TODO(gatis): verify data flows to table storage.
    Write-Host "* Done!"
}
catch [Exception]
{
  Write-Host $_
  exit -1
}
