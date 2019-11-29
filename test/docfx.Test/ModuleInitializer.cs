// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

// By default xunit limit max parallel threads to the number of CPU counts,
// this causes test hang on hosted CI servers where CPU count is small.
// Changing it to -1 to remove this limit.
[assembly: CollectionBehavior(MaxParallelThreads = -1)]

namespace Microsoft.Docs.Build
{
    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("appdata"));
            Environment.SetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH", Path.GetFullPath("docfx.test.yml"));

            Log.ForceVerbose = true;
            TestUtility.MakeDebugAssertThrowException();
        }
    }
}
