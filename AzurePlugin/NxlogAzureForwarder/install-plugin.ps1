# Stop on first error.
$ErrorActionPreference = "Stop"

Function FillTemplate($template, $file) {
  Get-Content $template | ForEach-Object {
    $([System.Environment]::ExpandEnvironmentVariables($_))
  } | Set-Content $file
}

Function Quote($path) {
  return '"' + $path + '"'
}

Write-Host "Setting environment variables."
$env:InstallPath = "${Env:ProgramFiles(x86)}\nxlog"
$env:WindowsEventQuery = $(Get-Content ($PSScriptRoot + "\WindowsEventQuery.xml")) -Join ""

Write-Host "Stopping nxlog..."
try {
  Stop-Service nxlog
} catch {}

Write-Host "Stopping NxlogAzureForwarder..."
try {
  Stop-Service NxlogAzureForwarder
} catch {}

Write-Host "Installing nxlog..."
Start-Process `
   -File ($PSScriptRoot + "\nxlog-ce-2.9.1347.msi") `
   -Arg "/quiet /norestart" `
   -Wait | Wait-Process

FillTemplate ($PSScriptRoot + "\nxlog.conf.template") ($env:InstallPath + "\conf\nxlog.conf")

Start-Process -File "sc" `
   -Arg "failure nxlog reset= 0 actions= restart/10000" `
   -NoNewWindow -Wait -PassThru | Wait-Process

Write-Host "Installing NxlogAzureForwarder..."
$forwarderExecSource = ($PSScriptRoot + "\NxlogAzureForwarder.exe")
$forwarderExecDestin = ($env:InstallPath + "\NxlogAzureForwarder.exe")
Copy-Item $forwarderExecSource $forwarderExecDestin
$appConfig = (Get-Content ($forwarderExecSource + ".config")) -As [Xml]
$appConfig.configuration.appSettings.add | foreach {
  try { $_.Value = [environment]::GetEnvironmentVariable($_.Key) } catch {}
}
$xmlOut = New-Object System.Xml.XmlTextWriter(($forwarderExecDestin + ".config"), [System.Text.Encoding]::UTF8)
$xmlOut.Formatting = [System.Xml.Formatting]::Indented
$appConfig.WriteContentTo($xmlOut)
$xmlOut.Close()

try {
Start-Process -File "sc" `
   -Arg "delete NxlogAzureForwarder" `
   -NoNewWindow -Wait | Wait-Process
} catch {}

$DotNetPath = $([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory())
Start-Process `
   -File ($DotNetPath + "\InstallUtil.exe") `
   -Arg ("/install " + (Quote $forwarderExecDestin)) `
   -NoNewWindow -Wait | Wait-Process

Start-Process -File "sc" `
   -Arg "failure NxlogAzureForwarder reset= 0 actions= restart/10000" `
   -NoNewWindow -Wait | Wait-Process

Write-Host "Restarting NxlogAzureForwarder..."
Restart-Service NxlogAzureForwarder

Write-Host "Restarting  nxlog..."
Restart-Service nxlog

# TODO(gatis): verify data flows to table storage.

Write-Host "Done."
