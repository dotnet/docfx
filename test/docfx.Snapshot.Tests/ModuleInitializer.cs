// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using DiffEngine;

namespace Docfx.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        BuildServerDetector.Detected = Environment.GetEnvironmentVariable("BUILD_SERVER") == "true";
        VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
    }
}
