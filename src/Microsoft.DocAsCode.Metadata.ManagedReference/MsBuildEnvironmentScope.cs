// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Build.Locator;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    using AsyncGenerator.Internal;

    public class MSBuildEnvironmentScope : IDisposable
    {
        private const string VSInstallDirKey = "VSINSTALLDIR";
        private const string MSBuildExePathKey = "MSBUILD_EXE_PATH";
        private static readonly Regex DotnetBasePathRegex = new Regex("Base Path:(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly EnvironmentScope _innerScope;

        public MSBuildEnvironmentScope()
        {
            _innerScope = GetScope();
        }

        private EnvironmentScope GetScope()
        {
            var vsInstallDirEnv = Environment.GetEnvironmentVariable(VSInstallDirKey);
            if (!string.IsNullOrEmpty(vsInstallDirEnv))
            {
                Logger.LogInfo($"Environment variable {VSInstallDirKey} is set to {vsInstallDirEnv}, it is used as the inner compiler.");
                return null;
            }

            var msbuildExePathEnv = Environment.GetEnvironmentVariable(MSBuildExePathKey);

            if (!string.IsNullOrEmpty(msbuildExePathEnv))
            {
                Logger.LogInfo($"Environment variable {MSBuildExePathKey} is set to {msbuildExePathEnv}, it is used as the inner compiler.");
                return null;
            }

            if (EnvironmentHelper.IsMono)
            {
                string extensionPath = null;
                string msbuildPath = null;
                EnvironmentHelper.GetMonoMsBuildPath(monoDir =>
                {
                    extensionPath = Path.Combine(monoDir, "xbuild");
                    msbuildPath = Path.Combine(monoDir, "msbuild", "15.0", "bin", "MSBuild.dll");
                });

                if (msbuildPath == null || !File.Exists(msbuildPath))
                {
                    var message = $"Unable to find msbuild from {msbuildPath}, please try downloading latest mono to solve the issue.";
                    Logger.LogError(message);
                    throw new DocfxException(message);
                }

                return new EnvironmentScope(new Dictionary<string, string>
                {
                    [MSBuildExePathKey] = msbuildPath,
                    ["MSBuildExtensionsPath"] = extensionPath
                });
            }

            try
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Count == 0)
                {
                    // when no visual studio installed, try detect dotnet
                    // workaround for https://github.com/dotnet/docfx/issues/1752
                    var dotnetBasePath = GetDotnetBasePath();
                    if (dotnetBasePath != null)
                    {
                        Logger.LogInfo($"Using dotnet {dotnetBasePath + "MSBuild.dll"} as inner compiler.");
                        return new EnvironmentScope(new Dictionary<string, string>
                        {
                            [MSBuildExePathKey] = dotnetBasePath + "MSBuild.dll",
                            ["MSBuildExtensionsPath"] = dotnetBasePath,
                            ["MSBuildSDKsPath"] = dotnetBasePath + "Sdks"
                        });
                    }
                }
                else
                {
                    // workaround for https://github.com/dotnet/docfx/issues/1969
                    // FYI https://github.com/dotnet/roslyn/issues/21799#issuecomment-343695700
                    var latest = instances.FirstOrDefault(a => a.Version.Major == 15);
                    if (latest != null)
                    {
                        Logger.LogInfo($"Using msbuild {latest.MSBuildPath} as inner compiler.");
                        MSBuildLocator.RegisterInstance(latest);
                        return new EnvironmentScope(new Dictionary<string, string>
                        {
                            [VSInstallDirKey] = latest.VisualStudioRootPath,
                            ["VisualStudioVersion"] = "15.0"
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogDiagnostic($"Have trouble locating MSBuild, if you meet issue similar to https://github.com/dotnet/docfx/issues/1969, try setting environment value VSINSTALLDIR and VisualStudioVersion as a workaround: {e.Message}");
            }

            return null;
        }

        private string GetDotnetBasePath()
        {
            using (var outputStream = new MemoryStream())
            {
                using (var outputStreamWriter = new StreamWriter(outputStream))
                {
                    try
                    {
                        CommandUtility.RunCommand(new CommandInfo
                        {
                            Name = "dotnet",
                            Arguments = "--info"
                        }, outputStreamWriter, timeoutInMilliseconds: 60000);
                    }
                    catch
                    {
                        // when error running dotnet command, consilder dotnet as not available
                        return null;
                    }

                    // writer streams have to be flushed before reading from memory streams
                    // make sure that streamwriter is not closed before reading from memory stream
                    outputStreamWriter.Flush();

                    var outputString = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());

                    var matched = DotnetBasePathRegex.Match(outputString);
                    if (matched.Success)
                    {
                        return matched.Groups[1].Value.Trim();
                    }

                    return null;
                }
            }
        }

        public void Dispose()
        {
            _innerScope?.Dispose();
        }
    }
}
