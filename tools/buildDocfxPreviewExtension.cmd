@ECHO OFF
PUSHD %~dp0
PowerShell -NoProfile -ExecutionPolicy Bypass -Command ".\buildDocfxPreviewExtension.ps1 %*; exit $LastExitCode;"
POPD