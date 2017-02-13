@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

:: Check if node exists globally
WHERE node >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateTemplate.cmd requires node installed globally.
    GOTO :Exit
)

SET TemplateHome=%~dp0src\docfx.website.themes\
SET DefaultTemplate=%TemplateHome%default\
SET GulpCommand=%DefaultTemplate%node_modules\gulp\bin\gulp

CD %DefaultTemplate%
CALL npm install
CALL node .\node_modules\bower\bin\bower install
CALL node %GulpCommand%

CD %TemplateHome%
CALL npm install
CALL node %GulpCommand%

:Exit
POPD

EXIT /B %ERRORLEVEL%'