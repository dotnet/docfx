// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Linq;

    using Microsoft.Build.MSBuildLocator;
    using Microsoft.DocAsCode.Common;

    public static class PrepareEnvironment
    {
        public static void Prepare()
        {
            // workaround for https://github.com/dotnet/docfx/issues/1969
            // FYI https://github.com/dotnet/roslyn/issues/21799#issuecomment-343695700
            var instances = MSBuildLocator.QueryVisualStudioInstances();
            var latest = instances.FirstOrDefault(a => a.Version.Major == 15);
            if (latest != null)
            {
                Logger.LogInfo($"Using msbuild {latest.MSBuildPath} as inner comipiler.");
                Environment.SetEnvironmentVariable("VSINSTALLDIR", latest.VisualStudioRootPath);
                Environment.SetEnvironmentVariable("VisualStudioVersion", "15.0");
            }
        }
    }
}
