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
if /I [%1]==[nonnetcore] (
    SET OnlyNonNetCore=true
    GOTO Next
)
REM TODO: remove it in next sprint
if /I [%1]==[nondnx] (
    SET OnlyNonNetCore=true
    GOTO Next
)

:Next
SHIFT /1
GOTO Loop

:Begin
IF NOT DEFINED VisualStudioVersion (
    IF DEFINED VS140COMNTOOLS (
        CALL "%VS140COMNTOOLS%\VsDevCmd.bat"
        GOTO EnvSet
    )

    ECHO Error: build.cmd requires Visual Studio 2015.
    SET ERRORLEVEL=1
    GOTO Exit
)

:EnvSet
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
    ECHO Updating template
    CALL UpdateTemplate.cmd
    IF NOT [!ERRORLEVEL!]==[0] (
        ECHO ERROR: Error occurs when updating template
    )
    GOTO Exit
)
IF /I [%SkipTemplate%]==[true] (
    ECHO Skip updating template
    GOTO CheckIfOnlyBuildNonDnx
)

:: Update template before build
:UpdateTemplate
ECHO Updating template
CALL UpdateTemplate.cmd
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: Error occurs when updating template
    GOTO Exit
)

:CheckIfOnlyBuildNonDnx
IF /I [%OnlyNonNetCore%]==[true] (
    ECHO Only build NonNETCore.sln
    SET BuildProj=%~dp0NonNETCore.sln
    CALL :RestoreNormalPackage
    GOTO SetBuildLog
)

:: Check if dotnet cli exists globally
:CheckDotnetCli
WHERE dotnet >NUL
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: dotnet CLI is not successfully configured.
    ECHO ERROR: Please follow https://www.microsoft.com/net/core to install .NET Core.
    GOTO Exit
)

:: Restore packages for .csproj projects

CALL :RestorePackage

:: Log build command line
:SetBuildLog
SET BuildLog=%~dp0msbuild.log
SET BuildPrefix=echo
SET BuildPostfix=^> "%BuildLog%"

CALL :Build %*

:: Build
SET BuildPrefix=
SET BuildPostfix=
CALL :Build %*

IF NOT [!ERRORLEVEL!]==[0] (
    GOTO AfterBuild
)

POPD

:AfterBuild
CALL :RunAllTest
CALL :GenerateArtifacts

:: Pull the build summary from the log file
ECHO.
ECHO === BUILD RESULT ===
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%BuildLog%" & cd >nul

:: Pull xunit test result from the log file
ECHO.
ECHO === TEST EXECUTION SUMMARY ===
findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%BuildLog%" & cd >nul

ECHO Exit Code: %BuildErrorLevel%
SET ERRORLEVEL=%BuildErrorLevel%

GOTO Exit

:Build
REM remove for dotnet cli bug https://github.com/dotnet/cli/issues/2871
REM dotnet build src\docfx -c %Configuration% -o target\%Configuration%\docfx.cli -f net452
dotnet build src\docfx -c %Configuration% -f net452
XCOPY /ey src\docfx\bin\%Configuration%\net452\win7-x64\** target\%Configuration%\docfx.cli\
%BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=d;LogFile="%BuildLog%"; %BuildPostfix%
SET BuildErrorLevel=%ERRORLEVEL%
EXIT /B %ERRORLEVEL%

:GenerateArtifacts
ECHO pack nuget package
FOR /f %%g IN ('DIR /b "src"') DO (
    CMD /C dotnet pack src\%%g -c Release -o artifacts\Release
)

:RunAllTest
ECHO run all test
FOR /f %%g IN ('DIR /b "test"') DO (
    IF NOT %%g==Shared (
        IF NOT %%g==docfx.E2E.Tests (
            CMD /C dotnet test test\%%g
        )
    )
)

:RestorePackage

:RestoreNormalPackage
:: Currently version 3.3 is not compatible with our build, force to use v2.8.6
SET CachedNuget=%LocalAppData%\NuGet\v2.8.6\NuGet.exe
IF EXIST "%CachedNuget%" GOTO Restore
ECHO Downloading NuGet.exe v2.8.6...
IF NOT EXIST "%LocalAppData%\NuGet\v2.8.6" MD "%LocalAppData%\NuGet\v2.8.6"
powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v2.8.6/nuget.exe' -OutFile '%CachedNuget%'"

IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: Failed downloading NuGet.exe
    GOTO Exit
)

:Restore
%CachedNuget% restore "%BuildProj%"
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: Error when restoring packages for %BuildProj%
)

:RestoreDnuPackage
FOR /D %%x IN ("src", "test") DO (
    PUSHD %%x
    CMD /C dotnet restore
    POPD
)

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
