#!/bin/bash
set -e
dotnet test test/docfx.Test -c Release
dotnet test test/docfx.FunctionalTest -c Release
dotnet pack src/docfx -c Release
