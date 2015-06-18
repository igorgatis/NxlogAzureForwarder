@echo off

pushd "%~dp0"
del /q /f ..\NAFInstaller.exe ..\AzurePlugin\NxlogAzureForwarder\NAFInstaller.exe
nsis-2.46\makensis.exe /X"SetCompressor /FINAL lzma" NxlogAzureForwarder.nsi
copy /y ..\NAFInstaller.exe ..\AzurePlugin\NxlogAzureForwarder
popd
