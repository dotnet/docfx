@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

SET Environment=%1
IF '%Environment%'=='' (
    SET Environment=Release
)

:: Check if node exists globally
WHERE node >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateVersion.cmd requires node installed gloablly.
    GOTO :Exit
)

:: Check if git exists globally
WHERE git >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateVersion.cmd requires git installed gloablly.
    GOTO :Exit
)

:GetBranch

FOR /F %%i in ('git rev-parse --abbrev-ref HEAD') DO (
    SET BRANCH=%%i
    ECHO CURRENT BRANCH: !BRANCH!
)

:GetVersion
SET MainVersion=
FOR /F "tokens=5 delims=:) " %%i in (RELEASENOTE.md) DO (
    SET MainVersion=%%i
    GOTO :GetGitVersion
)

IF NOT DEFINED MainVersion (
    ECHO ERROR: Unable to get main version from release note
    GOTO :Exit
)

:GetGitVersion
IF '%BRANCH%'=='master' (
    ECHO For master branch, use release version
    FOR /F "tokens=4 delims=.-" %%i in ('git describe') DO (
        SET VERSION=!MainVersion!.%%i
        ECHO CURRENT VERSION: !VERSION!
    )

) ELSE (
    ECHO For branch other than master, use alpha version
    FOR /F "tokens=4,5 delims=.-" %%i in ('git describe') DO (
        SET VERSION=!MainVersion!.%%i-alpha-%%j
        ECHO CURRENT VERSION:!VERSION!
    )
)

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: Unable to get version from git describe
    GOTO :Exit
)

:UpdateVersion
mkdir TEMP 2>NUL
ECHO %VERSION% > TEMP\version.txt
PUSHD tools
:: Install npm packages
CALL npm install

:: GRUNT to generate nuget packages
CALL node node_modules/grunt-cli/bin/grunt updateVersion --uv=%VERSION%

IF NOT '%ErrorLevel%'=='0' (
    ECHO ERROR: Unable to update version
    GOTO :Exit
)
POPD

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
