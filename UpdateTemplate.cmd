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
SET DefaultTemplate=%TemplateHome%default

CALL npm install -g gulp
CALL npm install -g bower

:: Check if gulp exists globally
WHERE gulp >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateTemplate.cmd requires gulp installed globally.
    GOTO :Exit
)

:: Check if bower exists globally
WHERE bower >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: UpdateTemplate.cmd requires bower installed globally.
    GOTO :Exit
)

CD %DefaultTemplate%
CALL npm install
CALL bower install
CALL gulp

CD %TemplateHome%
CALL npm install
CALL gulp

:Exit
POPD

EXIT /B %ERRORLEVEL%'