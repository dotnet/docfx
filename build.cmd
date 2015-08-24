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
SET DocsProj=%~dp0Docs.sln
SET Configuration=%1
IF '%Configuration%'=='' (
    SET Configuration=Release
)
SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe

:: node.js nuget wrapper requires nuget.exe path in %PATH%
SET PATH=%PATH%;%LocalAppData%\NuGet

:: Check if DNU exists globally
:: DNU is OPTIONAL
SET BuildDnxProjects=1
WHERE dnvm
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO WARNING: DNU is not installed globally, DNX related projects will not be built!
    SET BuildDnxProjects=0
    SET BuildProj=%~dp0NonDnx.sln
) ELSE (
  WHERE dnu
  IF NOT '!ERRORLEVEL!'=='0' (
    ECHO WARNING: DNU is correctly set, please manually select DNU by running `dnvm list` and `dnvm use`
    SET BuildDnxProjects=0
    SET BuildProj=%~dp0NonDnx.sln
  )
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
    GOTO :AfterBuild
)

CALL :BuildDocs %*
IF NOT '%ErrorLevel%'=='0' (
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
IF '%BuildDnxProjects%'=='0' (
   ECHO WARNING: DNX runtime is not successfully configured. Please follow http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm to install dnvm.
)

:: Pull xunit test result from the log file
ECHO.
ECHO === TEST EXECUTION SUMMARY ===
findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%BuildLog%" & cd >nul
ECHO Exit Code: %BuildErrorLevel%
SET ERRORLEVEL=%BuildErrorLevel%

:: Pull nuget package result
IF '%NugetErrorLevel%'=='1' (
    ECHO === GENERATING NUGET PACKAGES ===
    ECHO ERROR: GENERATING NUGET PACKAGES FAILED, TRY RUNNING `grunt` UNDER `tools` FOLDER MANUALLY!
    SET ERRORLEVEL=%NugetErrorLevel%
)

GOTO :Exit

:Build
%BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%BuildLog%"; %BuildPostfix% 
SET BuildErrorLevel=%ERRORLEVEL%
EXIT /B %BuildErrorLevel%

:BuildDocs
%BuildPrefix% msbuild "%DocsProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%BuildLog%";Append %BuildPostfix% 
SET DocsErrorLevel=%ERRORLEVEL%
EXIT /B %DocsErrorLevel%

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
