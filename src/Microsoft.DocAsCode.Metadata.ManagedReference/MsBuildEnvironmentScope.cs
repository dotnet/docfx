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

            if (Type.GetType("Mono.Runtime") != null) // is mono
            {
                var assembly = typeof(System.Runtime.GCSettings).Assembly;
                var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
                var monoDir = new DirectoryInfo(assemblyDirectory).Parent.FullName; // get mono directory

                var msbuildBasePath = Path.Combine(monoDir, "msbuild", "15.0", "bin");
                var msbuildPath = Path.Combine(msbuildBasePath, "MSBuild.dll");

                if (!File.Exists(msbuildPath))
                {
                    var message = $"Unable to find msbuild from {msbuildPath}, please try downloading latest mono to solve the issue.";
                    Logger.LogError(message);
                    throw new DocfxException(message);
                }

                Logger.LogInfo($"Using mono {msbuildPath} as inner compiler.");
                MSBuildLocator.RegisterMSBuildPath(msbuildBasePath);

                return new EnvironmentScope(new Dictionary<string, string>
                {
                    [MSBuildExePathKey] = msbuildPath,
                    ["MSBuildExtensionsPath"] = Path.Combine(monoDir, "xbuild"),
                    ["MSBuildSDKsPath"] = Path.Combine(msbuildBasePath, "Sdks")
                });
            }

            try
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();

                // workaround for https://github.com/dotnet/docfx/issues/1969
                // FYI https://github.com/dotnet/roslyn/issues/21799#issuecomment-343695700
                var latest = instances.FirstOrDefault(a => a.Version.Major >= 15);
                if (latest != null)
                {
                    Logger.LogInfo($"Using msbuild {latest.MSBuildPath} as inner compiler.");
                    MSBuildLocator.RegisterInstance(latest);
                    return new EnvironmentScope(new Dictionary<string, string>
                    {
                        ["VSINSTALLDIR"] = latest.VisualStudioRootPath,
                        ["VisualStudioVersion"] = latest.Version.ToString(2),
                    });
                }
                else
                {
                    Logger.LogWarning("Fail to find MSBuild >= 15.0 on machine. Please install Visual Studio 2017 or above with MSBuild >= 15.0: https://visualstudio.microsoft.com/vs/");
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
