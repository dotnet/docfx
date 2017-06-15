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
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class DependencyCommand : ISubCommand
    {
        private readonly DependencyCommandOptions _options;

        public string Name { get; } = nameof(DependencyCommand);

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
                LogErrorAndThrow($"Cache files in the folder {intermediateFolder} are corrupted!", null);
            }
            var dg = buildInfo.Versions.FirstOrDefault(v => v.VersionName == versionName)?.Dependency;
            if (dg == null)
            {
                Logger.LogInfo($"Cache files for version {versionName} is not found!", null);
            }
            Logger.LogInfo($"Exporting dependency file...");
            try
            {
                var edg = dg == null ? ExpandedDependencyMap.Empty : ExpandedDependencyMap.ConstructFromDependencyGraph(dg);
                using (var fs = File.Create(dependencyFile))
                using (var writer = new StreamWriter(fs))
                {
                    edg.Save(writer);
                }
                Logger.LogInfo($"Dependency file exported at path: {dependencyFile}.");
            }
            catch (Exception ex)
            {
                LogErrorAndThrow($"Unable to export dependency file: {ex.Message}", ex);
            }
        }

        private void LogErrorAndThrow(string message, Exception e)
        {
            Logger.LogError(message);
            throw new DocfxException(message, e);
        }
    }
}
