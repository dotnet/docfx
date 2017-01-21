// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Build.Engine.Incrementals.Outputs;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class DependencyCommand : ISubCommand
    {
        private readonly DependencyCommandOptions _options;

        public bool AllowReplay => true;

        public DependencyCommand(DependencyCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            string intermediateFolder = _options.IntermediateFolder;
            string versionName = _options.VersionName ?? string.Empty;
            var dependencyFile = string.IsNullOrEmpty(_options.DependencyFile) ? Path.Combine(Directory.GetCurrentDirectory(), "dep.json") : _options.DependencyFile;
            var buildInfo = BuildInfo.Load(intermediateFolder);
            if (buildInfo == null)
            {
                Logger.LogWarning($"Cache files in the folder {intermediateFolder} are corrupted!");
                return;
            }
            var dg = buildInfo.Versions.FirstOrDefault(v => v.VersionName == versionName)?.Dependency;
            if (dg == null)
            {
                Logger.LogWarning($"Cache files for version {versionName} is not found!");
                return;
            }
            Logger.LogInfo($"Exporting dependency file...");
            try
            {
                var edg = ExpandedDependencyMap.ConstructFromDependencyGraph(dg);
                using (var fs = File.Create(dependencyFile))
                using (var writer = new StreamWriter(fs))
                {
                    edg.Save(writer);
                }
                Logger.LogInfo($"Dependency file exported at path: {dependencyFile}.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unable to export dependency file: {ex}");
            }
        }
    }
}
