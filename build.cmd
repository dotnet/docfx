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
if /I [%1]==[nondnx] (
    SET OnlyNonDnx=true
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
IF /I [%OnlyNonDnx%]==[true] (
    ECHO Only build NonDnx.sln
    SET BuildProj=%~dp0NonDNX.sln
    GOTO SetBuildLog
)

:: Check if DNU exists globally
:: DNU is OPTIONAL
:CheckDnu
WHERE dnu >NUL
IF NOT [!ERRORLEVEL!]==[0] (
    ECHO ERROR: DNU is not successfully configured.
    ECHO ERROR: Please follow http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm to install dnvm.
    ECHO ERROR: If dnvm is installed, use `dnvm list` to show available dnx runtime, and use `dnvm use` to select the default dnx runtime
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
%BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=d;LogFile="%BuildLog%"; %BuildPostfix%
SET BuildErrorLevel=%ERRORLEVEL%
EXIT /B %ERRORLEVEL%

:RestorePackage

:RestoreDnuPackage
FOR /D %%x IN ("src","test","tools") DO (
    PUSHD %%x
    CMD /C dnu restore --parallel
    POPD
)

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
    GOTO Exit
)

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
