@ECHO OFF
PUSHD %~dp0

:: Restore
REM SET srcHome="src/"
REM PUSHD %srcHome%
REM CMD /C dotnet restore
REM POPD

:: Build
SET docfxHome="src/docfx/"
PUSHD %docfxHome%
CMD /C dotnet pack -o artifacts/ 
POPD

POPD