#!/bin/bash
set -e
dotnet run -p tools/CreateJsonSchema
dotnet test test/docfx.Test 2>&1
dotnet test test/docfx.Test -c Release 2>&1


rm -rf drop
dotnet pack src/docfx -c Release -o $PWD/drop /p:Version=3.0.0
dotnet tool install docfx --version 3.0.0-* --add-source drop --tool-path drop