// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Locator;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    public class MSBuildEnvironmentScope : IDisposable
    {
        private const string MSBuildExePathKey = "MSBUILD_EXE_PATH";
        private readonly EnvironmentScope _innerScope;

        public MSBuildEnvironmentScope()
        {
            _innerScope = GetScope();
        }

        private EnvironmentScope GetScope()
        {
            var msbuildExePathEnv = Environment.GetEnvironmentVariable(MSBuildExePathKey);

            if (!string.IsNullOrEmpty(msbuildExePathEnv))
            {
                Logger.LogInfo($"Environment variable {MSBuildExePathKey} is set to {msbuildExePathEnv}, it is used as the inner compiler.");
                return null;
            }

            try
            {
                // workaround for https://github.com/dotnet/docfx/issues/1969
                // FYI https://github.com/dotnet/roslyn/issues/21799#issuecomment-343695700
                IEnumerable<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances();
                VisualStudioInstance latestVS =
                    instances?.Any() ?? false
                    ? instances.Aggregate((vs1, vs2) => vs1.Version > vs2.Version ? vs1 : vs2)
                    : null;

                if (latestVS?.Version.Major >= 15)
                {
                    Logger.LogInfo($"Using msbuild {latestVS.MSBuildPath} as inner compiler.");
                    MSBuildLocator.RegisterInstance(latestVS);
                    return new EnvironmentScope(new Dictionary<string, string>
                    {
                        ["VSINSTALLDIR"] = latestVS.VisualStudioRootPath,
                        // VS2019 defines VisualStudioVersion=16.0. Do not include minor version here to avoid breaking UWP projects.
                        // See: https://github.com/dotnet/docfx/issues/5011
                        ["VisualStudioVersion"] = $"{latestVS.Version.Major}.0",
                    });
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.LogDiagnostic($"Have trouble locating MSBuild, if you meet issue similar to https://github.com/dotnet/docfx/issues/1969, try setting environment value VSINSTALLDIR and VisualStudioVersion as a workaround: {e.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            _innerScope?.Dispose();
        }
    }
}
