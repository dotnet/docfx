#!/bin/bash
dotnet test test/docfx.Test
dotnet publish src/docfx -c Release