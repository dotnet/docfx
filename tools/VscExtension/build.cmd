@ECHO OFF
PUSHD %~dp0
PowerShell -NoProfile -ExecutionPolicy Bypass -Command ".\build.ps1 %*; exit $LastExitCode;"
POPD