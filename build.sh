#!/bin/bash
set -e

git rev-parse --abbrev-ref HEAD
git config --get remote.origin.url

dotnet test test/docfx.Test
dotnet test test/docfx.Test -c Release
dotnet pack src/docfx -c Release /p:PackAsTool=true
