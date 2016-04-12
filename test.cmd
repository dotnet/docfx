@ECHO OFF
PUSHD %~dp0

FOR /D %%x IN ("src/*","tools/*") DO (
    ECHO %%x
)
