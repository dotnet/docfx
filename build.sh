#!/bin/bash
set -e
dotnet run -p tools/CreateJsonSchema
dotnet test test/docfx.Test
dotnet test test/docfx.Test -c Release
dotnet pack src/docfx -c Release /p:PackAsTool=true
