@echo off
pushd "%~dp0"
nsis-2.46\makensis.exe /X"SetCompressor /FINAL lzma" NxlogAzureForwarder.nsi
copy /y ..\NAFInstaller.exe ..\AzurePlugin\NxlogAzureForwarder