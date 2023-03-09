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
            // Disable for PR build to allow commiting diff files
            BuildServerDetector.Detected = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") == "push";
            VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
        }
    }
}
