﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Build.MSBuildLocator;
    using Microsoft.DocAsCode.Common;

    public class MSBuildEnvironmentScope : IDisposable
    {
        private static readonly Regex DotnetBasePathRegex = new Regex("Base Path:(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly EnvironmentScope _innerScope;

        public MSBuildEnvironmentScope()
        {
            // workaround for https://github.com/dotnet/docfx/issues/1752
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            if (instances.Count == 0)
            {
                // when no visual studio installed, try detect dotnet
                var dotnetBasePath = GetDotnetBasePath();
                if (dotnetBasePath != null)
                {
                    Logger.LogInfo($"Using dotnet {dotnetBasePath} as inner comipiler.");
                    _innerScope = new EnvironmentScope(new Dictionary<string, string>
                    {
                        ["MSBuild_EXE_PATH"] = dotnetBasePath + "MSBuild.dll",
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
                    Logger.LogInfo($"Using msbuild {latest.MSBuildPath} as inner comipiler.");

                    _innerScope = new EnvironmentScope(new Dictionary<string, string>
                    {
                        ["VSINSTALLDIR"] = latest.VisualStudioRootPath,
                        ["VisualStudioVersion"] = "15.0"
                    });
                }
            }
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
