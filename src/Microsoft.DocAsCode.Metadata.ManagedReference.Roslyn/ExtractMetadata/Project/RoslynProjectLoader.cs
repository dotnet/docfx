// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.CodeAnalysis.MSBuild;

    using Microsoft.DocAsCode.Common;


    public class RoslynProjectLoader : IProjectLoader
    {
        Lazy<MSBuildWorkspace> _workspace;

        public RoslynProjectLoader(Lazy<MSBuildWorkspace> workspace)
        {
            _workspace = workspace;
        }

        public AbstractProject TryLoad(string path, AbstractProjectLoader loader)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".csproj" || ext == ".vbproj")
            {
                Logger.LogVerbose("Loading project...");
                var project = _workspace.Value.CurrentSolution.Projects.FirstOrDefault(
                    p => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(p.FilePath, path));
                var result = project ?? _workspace.Value.OpenProjectAsync(path).Result;
                Logger.LogVerbose($"Project {result.FilePath} loaded.");
                return new RoslynProject(result);
            }
            else
                return null;
        }
    }
}
