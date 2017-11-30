// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Build.MSBuildLocator;
    using Microsoft.DocAsCode.Common;

    public class MSBuildEnvironmentScope : IDisposable
    {
        private readonly EnvironmentScope _innerScope;

        public MSBuildEnvironmentScope()
        {
            // workaround for https://github.com/dotnet/docfx/issues/1969
            // FYI https://github.com/dotnet/roslyn/issues/21799#issuecomment-343695700
            var instances = MSBuildLocator.QueryVisualStudioInstances();
            var latest = instances.FirstOrDefault(a => a.Version.Major == 15);
            if (latest != null)
            {
                Logger.LogInfo($"Using msbuild {latest.MSBuildPath} as inner compiler.");

                _innerScope = new EnvironmentScope(new Dictionary<string, string>
                {
                    ["VSINSTALLDIR"] = latest.VisualStudioRootPath,
                    ["VisualStudioVersion"] = "15.0"
                });
            }
        }

        public void Dispose()
        {
            _innerScope?.Dispose();
        }
    }
}
