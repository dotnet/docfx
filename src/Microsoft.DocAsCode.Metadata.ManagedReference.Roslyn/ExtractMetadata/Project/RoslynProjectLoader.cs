using System;
using System.IO;

using Microsoft.CodeAnalysis.MSBuild;

using Microsoft.DocAsCode.Common;


namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
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
                var result = _workspace.Value.OpenProjectAsync(path).Result;
                Logger.LogVerbose($"Project {result.FilePath} loaded.");
                return new RoslynProject(result);
            }
            else
                return null;
        }
    }
}
