#!/bin/bash
set -e
dotnet run -p tools/CreateJsonSchema
dotnet test test/docfx.Test
dotnet test test/docfx.Test -c Release
dotnet pack src/docfx -c Release

toolPath = drop\tools\$(date +%s)
dotnet tool install docfx --version 3.0.0-* --add-source drop --tool-path $toolPath

pushd $toolPath
docfx --version
popd
