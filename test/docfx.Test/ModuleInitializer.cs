// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("appdata"));
            Environment.SetEnvironmentVariable("DOCFX_BASE_URL", "https://docs.com");
            Environment.SetEnvironmentVariable("DOCFX_OUTPUT__JSON", "true");
            Environment.SetEnvironmentVariable("DOCFX_OUTPUT__COPY_RESOURCES", "true");

            Log.ForceVerbose = true;
            TestUtility.MakeDebugAssertThrowException();
        }
    }
}
