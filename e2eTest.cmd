@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

:EnvSet
SET BuildProj=%~dp0docfx.E2E.Tests.sln
SET TestTargetProj=%~dp0\..\docfx-seed\docfx.json
SET Configuration=%1
IF '%Configuration%'=='' (
    SET Configuration=Release
)
SET DocfxExe=%~dp0target\%Configuration%\docfx\docfx.exe
SET Environment=%2
IF '%Environment%'=='' (
    SET Environment=TEST
)

:: check if Firefox browser exists
REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe" >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: e2eTest.cmd requires Firefox.
    GOTO :Exit
)

:: check if test target project exists 
IF NOT EXIST %TestTargetProj% (
    ECHO ERROR: docfx-seed not found. Please clone docfx-seed repo to "..\docfx-seed". URL: https://github.com/docascode/docfx-seed.git
    SET ERRORLEVEL=1
    GOTO :Exit
)

:: 1.build docfx.exe 2.build test site 3.run e2e tests

CALL build.cmd %Configuration% %Environment% %BuildProj%

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
