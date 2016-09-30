@ECHO OFF
PUSHD %~dp0
PowerShell -NoProfile -ExecutionPolicy Bypass -Command ".\buildVscPreviewExe.ps1 %*; exit $LastExitCode;"
POPD