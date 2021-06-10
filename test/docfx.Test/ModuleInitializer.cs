// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

// By default xunit limit max parallel threads to the number of CPU counts,
// this causes test hang on hosted CI servers where CPU count is small.
// Changing it to -1 to remove this limit.
[assembly: CollectionBehavior(MaxParallelThreads = -1)]

namespace Microsoft.Docs.Build
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("appdata"));
            Environment.SetEnvironmentVariable("DOCFX_HOST_NAME", "docs.com");
            Environment.SetEnvironmentVariable("DOCFX_OUTPUT_TYPE", "Json");
            Environment.SetEnvironmentVariable("DOCFX_URL_TYPE", "Docs");
            Environment.SetEnvironmentVariable("DOCFX_MICROSOFT_GRAPH_TENANT_ID", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            Environment.SetEnvironmentVariable("DOCFX_MICROSOFT_GRAPH_CLIENT_ID", "b799e059-9dd8-4839-a39c-96f7531e55e2");
            Environment.SetEnvironmentVariable("DOCS_ENVIRONMENT", "PPE");

            TestQuirks.Verbose = true;
            TestUtility.MakeDebugAssertThrowException();
        }
    }
}
