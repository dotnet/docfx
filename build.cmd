@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

:Loop
if [%1]==[] GOTO Begin

if /I [%1]==[Debug] (
    SET Configuration=%1
    GOTO Next
)

if /I [%1]==[Release] (
    SET Configuration=%1
    GOTO Next
)

if /I [%1]==[PROD] (
    SET Environment=PROD
    GOTO Next
)

if /I [%1]==[raw] (
    SET SkipTemplate=true
    GOTO Next
)

if /I [%1]==[template] (
    SET UpdateTemplate=%1
    GOTO Next
)

:Next
SHIFT /1
GOTO Loop

:Begin
:: Check if dotnet cli exists globally
WHERE dotnet >NUL
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: dotnet CLI is not successfully configured.
    ECHO ERROR: Please follow https://www.microsoft.com/net/core to install .NET Core.
    GOTO Exit
)
:: Check if nuget.exe exists globally
SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe
IF NOT EXIST "%CachedNuget%" (
    ECHO Downloading NuGet.exe...
    powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile '%CachedNuget%'"
)

:: Set environment variable
SET BuildProj=%~dp0All.sln
IF [%Configuration%]==[] (
    SET Configuration=Release
)

IF /I [%Environment%]==[PROD] (
    ECHO Updating version for PROD environment
    CALL UpdateVersion.cmd

    IF NOT [!ERRORLEVEL!]==[0] (
        ECHO ERROR: Error occurs when updating version
        GOTO Exit
    )
)

IF /I [%UpdateTemplate%]==[template] (
    CALL :UpdateTemplate
    GOTO Exit
)

IF /I [%SkipTemplate%]==[true] (
    ECHO Skip updating template
    GOTO RestorePackage
)

:: Update template before build
:UpdateTemplate
ECHO Updating template
CALL UpdateTemplate.cmd
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: Error occurs when updating template
    GOTO Exit
)

:RestorePackage
FOR /D %%x IN ("src", "test") DO (
    PUSHD %%x
    dotnet restore
    POPD
)

:BuildProject
ECHO Building project
FOR /f %%g IN ('DIR /b "src"') DO (
    dotnet build src\%%g -c %Configuration% -f net452
    XCOPY /ey src\%%g\bin\%Configuration%\net452\win7-x64\** target\%Configuration%\%%g\
)

:RunUnitTests
ECHO Run all unit tests
FOR /f %%g IN ('DIR /b "test"') DO (
    IF NOT %%g==Shared (
        IF NOT %%g==docfx.E2E.Tests (
            dotnet test test\%%g
        )
    )
)

:GenerateArtifacts
ECHO pack projct nuget package
FOR /f %%g IN ('DIR /b "src"') DO (
    dotnet pack src\%%g -c %Configuration% -o artifacts\%Configuration%
)

ECHO pack docfx.conosle
ECHO XCOPY /ey target\%Configuration%\docfx\*.dll src\nuspec\docfx.console\tools\
XCOPY /ey target\%Configuration%\docfx\*.dll src\nuspec\docfx.console\tools\
XCOPY /ey target\%Configuration%\docfx\*.exe src\nuspec\docfx.console\tools\
XCOPY /ey target\%Configuration%\docfx\*.exe.config src\nuspec\docfx.console\tools\
SET versionFile=TEMP/version.txt
IF EXIST %versionFile% (
    SET /p versions=<%versionFile%
    SET version=!versions:~1!
) ELSE (
    SET version=1.0.0
)
%CachedNuget% pack src\nuspec\docfx.console\docfx.console.nuspec -Version %version% -OutputDirectory artifacts\%Configuration%

:Exit
POPD
ECHO.
EXIT /B %ERRORLEVEL%