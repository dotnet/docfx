@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

:: Check if node exists globally
WHERE node >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateVersion.cmd requires node installed globally.
    GOTO :Exit
)

:: Check if git exists globally
WHERE git >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateVersion.cmd requires git installed globally.
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

:GetGitVersion
IF NOT DEFINED MainVersion (
    ECHO ERROR: Unable to get main version from release note
    GOTO :Exit
)

FOR /F "tokens=3 delims=." %%i in ("!MainVersion!") DO (
    SET BuildVersionExists=1
)

IF NOT DEFINED BuildVersionExists (
    SET MainVersion=!MainVersion!.0
)

ECHO CURRENT MAINVERSION: !MainVersion!

IF '%BRANCH%'=='master' (
    ECHO For master branch, use release version
    SET VERSION=!MainVersion!
) ELSE (
    ECHO For branch other than master, use alpha version
    FOR /F "tokens=2,3 delims=-" %%i in ('git describe') DO (
        SET PADDINGZERO=0000
        SET VERSIONI=%%i
        SET VERSIONJ=%%j
        SET TMP=%%i
        :EXECUTE
        IF DEFINED TMP (
        SET PADDINGZERO=!PADDINGZERO:~1!
        SET TMP=!TMP:~1!
        GOTO EXECUTE
        )
        SET VERSION=!MainVersion!-alpha-!PADDINGZERO!!VERSIONI!-!VERSIONJ!

        ECHO CURRENT VERSION:!VERSION!
    )
)

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: Unable to get version from git describe
    GOTO :Exit
)

:UpdateVersion
mkdir TEMP 2>NUL
ECHO %VERSION%> TEMP\version.txt
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
