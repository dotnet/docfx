// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Xunit;

// By default xunit limit max parallel threads to the number of CPU counts,
// this causes test hang on hosted CI servers where CPU count is small.
// Changing it to -1 to remove this limit.
[assembly: CollectionBehavior(MaxParallelThreads = -1)]

namespace Microsoft.Docs.Build;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("appdata"));
        Environment.SetEnvironmentVariable("DOCFX_HOST_NAME", "docs.com");
        Environment.SetEnvironmentVariable("DOCFX_OUTPUT_TYPE", "Json");
        Environment.SetEnvironmentVariable("DOCFX_URL_TYPE", "Docs");
        Environment.SetEnvironmentVariable("DOCS_ENVIRONMENT", "PPE");

        TestQuirks.Verbose = true;
        TestUtility.MakeDebugAssertThrowException();
    }
}
