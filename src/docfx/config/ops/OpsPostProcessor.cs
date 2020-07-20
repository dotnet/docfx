// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Docs.LearnValidation;

namespace Microsoft.Docs.Build
{
    internal class OpsPostProcessor
    {
        private static readonly object s_lock = new object();

        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly ErrorLog _errorLog;

        public OpsPostProcessor(Config config, ErrorLog errorLog, BuildOptions buildOptions)
        {
            _config = config;
            _errorLog = errorLog;
            _buildOptions = buildOptions;
        }

        public void Run()
        {
            PostProcessLearnValidation();
        }

        private void PostProcessLearnValidation()
        {
            if (!_config.RunLearnValidation)
            {
                return;
            }

            using (Progress.Start("Postprocessing learn contents"))
            {
                lock (s_lock)
                {
                    LearnValidationEntry.Run(
                        repoUrl: _buildOptions.Repository?.Remote,
                        repoBranch: _buildOptions.Repository?.Branch,
                        docsetName: _config.Name,
                        docsetPath: _buildOptions.DocsetPath,
                        publishFilePath: Path.GetFullPath(Path.Combine(_buildOptions.OutputPath, ".publish.json")),
                        dependencyFilePath: Path.GetFullPath(Path.Combine(_buildOptions.OutputPath, "full-dependent-list.txt")),
                        manifestFilePath: Path.GetFullPath(Path.Combine(_buildOptions.OutputPath, _config.BasePath, ".manifest.json")),
                        environment: OpsConfigAdapter.DocsEnvironment.ToString(),
                        isLocalizationBuild: _buildOptions.IsLocalizedBuild,
                        writeLog: LogError,
                        fallbackDocsetPath: _buildOptions.FallbackDocsetPath);
                }
            }
        }

        private void LogError(LearnLogItem item)
        {
            _errorLog.Write(
                new Error(MapLevel(item.ErrorLevel), item.ErrorCode.ToString(), item.Message, item.File is null ? null : new FilePath(item.File), 0));

            ErrorLevel MapLevel(LearnErrorLevel level)
                => level switch
                {
                    LearnErrorLevel.Error => ErrorLevel.Error,
                    LearnErrorLevel.Warning => ErrorLevel.Warning,
                    _ => ErrorLevel.Off,
                };
        }
    }
}
