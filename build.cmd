@ECHO OFF
PUSHD %~dp0
pwsh -NoProfile -ExecutionPolicy Bypass -Command ".\build.ps1 %*; exit $LastExitCode;"
POPD