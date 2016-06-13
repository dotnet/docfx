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

PUSHD %DefaultTemplate%
CALL npm install
CALL gulp
POPD

PUSHD %TemplateHome%
CALL npm install
CALL gulp
POPD
