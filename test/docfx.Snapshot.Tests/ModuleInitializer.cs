// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using DiffEngine;

namespace Microsoft.DocAsCode.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        BuildServerDetector.Detected = Environment.GetEnvironmentVariable("BUILD_SERVER") == "true";
        VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
    }
}
