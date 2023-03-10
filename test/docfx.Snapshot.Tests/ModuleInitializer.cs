// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Runtime.CompilerServices;
    using DiffEngine;
    using VerifyTests;

    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            BuildServerDetector.Detected = Environment.GetEnvironmentVariable("BUILD_SERVER") == "true";
            VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
        }
    }
}
