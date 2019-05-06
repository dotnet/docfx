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
            var outputFile = string.IsNullOrEmpty(_options.DependencyFile) ? Path.Combine(Directory.GetCurrentDirectory(), "dep.json") : _options.DependencyFile;

            var dependency = Load(intermediateFolder, versionName);
            Logger.LogInfo($"Exporting dependency file...");
            try
            {
                var expandedDependency = dependency == null ?
                    ExpandedDependencyMap.Empty :
                    ExpandedDependencyMap.ConstructFromDependencyGraph(dependency);
                using (var fs = File.Create(outputFile))
                using (var writer = new StreamWriter(fs))
                {
                    expandedDependency.Save(writer);
                }
                Logger.LogInfo($"Dependency file exported at path: {outputFile}.");
            }
            catch (Exception ex)
            {
                LogErrorAndThrow($"Unable to export dependency file: {ex.Message}", ex);
            }
        }

        private DependencyGraph Load(string intermediateFolder, string versionName)
        {
            var expandedBaseFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(intermediateFolder));
            var buildInfoFile = Path.Combine(expandedBaseFolder, BuildInfo.FileName);
            var buildInfo = JsonUtility.Deserialize<BuildInfo>(buildInfoFile);
            if (buildInfo == null)
            {
                LogErrorAndThrow($"Cache files in the folder '{intermediateFolder}' are corrupted!", null);
            }
            var versionInfo = buildInfo.Versions.FirstOrDefault(v => v.VersionName == versionName);
            if (versionInfo == null)
            {
                Logger.LogInfo($"Cache files for version '{versionName}' is not found!", null);
                return null;
            }
            var dependencyFile = Path.Combine(expandedBaseFolder, buildInfo.DirectoryName, versionInfo.DependencyFile);
            return IncrementalUtility.LoadDependency(dependencyFile);
        }

        private void LogErrorAndThrow(string message, Exception e)
        {
            Logger.LogError(message);
            throw new DocfxException(message, e);
        }
    }
}
