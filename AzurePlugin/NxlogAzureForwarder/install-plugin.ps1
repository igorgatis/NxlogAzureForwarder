# Stop on first error.
$ErrorActionPreference = "Stop"

Function FillNxlogConf
{
    Get-Content "$PSScriptRoot\nxlog.conf.template" `
        | ForEach-Object { $([System.Environment]::ExpandEnvironmentVariables($_)) } `
        | Set-Content "$PSScriptRoot\nxlog.conf"
}

Function FillNxlogAzureForwarderConfig
{
    $templateFile = "$PSScriptRoot\NxlogAzureForwarder.exe.config.template"
    $appConfig = (Get-Content $templateFile) -As [Xml]
    $appConfig.configuration.appSettings.add `
        | ForEach { try { $_.Value = [environment]::GetEnvironmentVariable($_.Key) } catch {} }
    $configFile = "$PSScriptRoot\NxlogAzureForwarder.exe.config"
    $xmlOut = New-Object System.Xml.XmlTextWriter($configFile, [System.Text.Encoding]::UTF8)
    $xmlOut.Formatting = [System.Xml.Formatting]::Indented
    $appConfig.WriteContentTo($xmlOut)
    $xmlOut.Close()
}

Write-Host "Filling up config files."
$env:NxlogInstallPath = "${Env:ProgramFiles(x86)}\nxlog"
$env:WindowsEventQuery = $(Get-Content "$PSScriptRoot\WindowsEventQuery.xml") -Join ""

FillNxlogConf
FillNxlogAzureForwarderConfig

Write-Host "Installing Nxlog and NxlogAzureForwarder"
Start-Process -File "$PSScriptRoot\NAFInstaller.exe" `
    -Arg "/S" `
    -NoNewWindow -Wait | Wait-Process

# TODO(gatis): verify data flows to table storage.

Write-Host "Done."
