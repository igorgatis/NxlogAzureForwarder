Name "NxlogAzureForwarder"

OutFile "..\NAFInstaller.exe"
InstallDir "$PROGRAMFILES\NxlogAzureForwarder"
RequestExecutionLevel admin

Section "Install"
  SectionIn RO
  SetOutPath $INSTDIR
  File "NxlogAzureForwarder.exe"
  File "NxlogAzureForwarder.exe.config"
  File "nxlog-ce-2.9.1347.msi"
  File "nxlog.conf"
  File "setup.ps1"

  ; Override for NxlogAzureForwarder.exe.config.
  IfFileExists "$EXEDIR\NxlogAzureForwarder.exe.config" CopyNAFConfig SkipNAFConfig
  CopyNAFConfig:
    CopyFiles "$EXEDIR\NxlogAzureForwarder.exe.config" "$INSTDIR\NxlogAzureForwarder.exe.config"
  SkipNAFConfig:

  ; Override for nxlog.conf.
  IfFileExists "$EXEDIR\nxlog.conf" CopyNxlogConf SkipNxlogConf
  CopyNxlogConf:
    CopyFiles "$EXEDIR\nxlog.conf" "$INSTDIR\nxlog.conf"
  SkipNxlogConf:

  DetailPrint "Running post install scripts..."
  nsExec::ExecToLog 'PowerShell.exe -ExecutionPolicy UnRestricted -File "$INSTDIR\setup.ps1"'
  Pop $R0
  IntCmp $R0 0 SetupSuccess
    SetDetailsView show
    Abort "INSTALLATION FAILED!"
  SetupSuccess:
    DetailPrint "SUCCESS!"

SectionEnd
