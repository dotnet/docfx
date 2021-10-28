@ECHO OFF
PUSHD %~dp0
pwsh -NoProfile -ExecutionPolicy Bypass -Command ".\UpdateTemplate.ps1 %*; exit $LastExitCode;"
POPD
