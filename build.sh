#!/bin/bash
set -e
dotnet test test/docfx.Test
dotnet test test/docfx.Test -c Release
dotnet pack src/docfx -c Release
