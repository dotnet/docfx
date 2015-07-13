@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

IF NOT DEFINED VisualStudioVersion (
    IF DEFINED VS140COMNTOOLS (
        CALL "%VS140COMNTOOLS%\VsDevCmd.bat"
        GOTO :EnvSet
    )

    ECHO Error: build.cmd requires Visual Studio 2015.
    SET ERRORLEVEL=1
    GOTO :Exit
)

:EnvSet
SET BuildProj=%~dp0All.sln
SET Configuration=Debug
SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe

:: node.js nuget wrapper requires nuget.exe path in %PATH%
SET PATH=%PATH%;%LocalAppData%\NuGet

:: Check if DNU exists globally
:: DNU is OPTIONAL
SET BuildDnxProjects=1
WHERE dnu
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO WARNING: DNU is not installed globally, DNX related projects will not be built!
    SET BuildDnxProjects=0
    SET BuildProj=%~dp0NonDnx.sln
)

:: Check if node exists globally
WHERE node

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: build.cmd requires node installed gloablly. 
    GOTO :Exit
)

:: Restore packages for .csproj projects

CALL :RestorePackage

:: Log build command line
SET BuildLog=%~dp0msbuild.log
SET BuildPrefix=echo
SET BuildPostfix=^> "%BuildLog%"

CALL :Build %*

:: Build
SET BuildPrefix=
SET BuildPostfix=
CALL :Build %*
IF NOT '%ErrorLevel%'=='0' (
    ECHO Exit Code: %ErrorLevel%
    GOTO :AfterBuild
)

:GenerateNuget
PUSHD tools
:: Install npm packages
CALL npm install

:: GRUNT to generate nuget packages
CALL node node_modules/grunt-cli/bin/grunt --Configuration=%Configuration%

IF NOT '%ErrorLevel%'=='0' (
    SET NugetErrorLevel=1
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

:: Pull nuget package result
IF '%NugetErrorLevel%'=='1' (
    ECHO === GENERATING NUGET PACKAGES ===
    ECHO ERROR: GENERATING NUGET PACKAGES FAILED, TRY RUNNING `grunt` UNDER `tools` FOLDER MANUALLY!
)
GOTO :Exit

:Build
%BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%BuildLog%";Append %* %BuildPostfix%
SET BuildErrorLevel=%ERRORLEVEL%
GOTO :Exit

:RestorePackage
:: Restore inside each subfolder
IF '%BuildDnxProjects%'=='0' (
GOTO :RestoreNormalPackage
)

FOR /D %%x IN ("src","docs","test") DO (
PUSHD %%x
CMD /C dnu restore --parallel
POPD
)

:RestoreNormalPackage
SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe
IF EXIST "%CachedNuget%" GOTO :Restore
ECHO Downloading latest version of NuGet.exe...
IF NOT EXIST "%LocalAppData%\NuGet" MD "%LocalAppData%\NuGet"
powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CachedNuget%'"

:Restore
:: Currently has corpnet dependency
nuget restore "%BuildProj%"

:Exit
POPD
ECHO.
EXIT /B %ERRORLEVEL%
