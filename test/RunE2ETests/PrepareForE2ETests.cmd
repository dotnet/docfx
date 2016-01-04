@ECHO OFF
PUSHD %~dp0

SET TestTargetProj=%~dp0\..\..\..\docfx-seed\docfx.json

:: check if Firefox browser exists
REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe" >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: e2eTest.cmd requires Firefox.
    GOTO :Exit
)

:: check if test target project exists 
IF NOT EXIST %TestTargetProj% (
    ECHO ERROR: docfx-seed not found. Please clone docfx-seed repo to "..\..\..\docfx-seed". URL: https://github.com/docascode/docfx-seed.git
    SET ERRORLEVEL=1
    GOTO :Exit
)


:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
