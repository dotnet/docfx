@ECHO OFF
PUSHD %~dp0
PowerShell -NoProfile -ExecutionPolicy Bypass -Command ".\UpdateTemplate.ps1 %*; exit $LastExitCode;"
POPD
