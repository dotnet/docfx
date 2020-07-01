// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using ECMA2Yaml;

namespace Microsoft.Docs.Build
{
    internal class OpsPreProcessor
    {
        private static readonly object s_lock = new object();

        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly ErrorLog _errorLog;

        public OpsPreProcessor(Config config, ErrorLog errorLog, BuildOptions buildOptions)
        {
            _config = config;
            _errorLog = errorLog;
            _buildOptions = buildOptions;
        }

        public void Run()
        {
            PreProcessMonoDocXml();
        }

        private void PreProcessMonoDocXml()
        {
            if (_config.Monodoc is null)
            {
                return;
            }

            using (Progress.Start("Preprocessing monodoc XML files"))
            {
                lock (s_lock)
                {
                    for (var index = 0; index < _config.Monodoc.Length; index++)
                    {
                        var monodocConfig = _config.Monodoc[index];
                        if (Directory.Exists(monodocConfig.OutputYamlFolder))
                        {
                            Directory.Delete(monodocConfig.OutputYamlFolder, recursive: true);
                        }

                        Directory.CreateDirectory(monodocConfig.OutputYamlFolder);

                        var fallbackXmlPath = _buildOptions.FallbackDocsetPath is null
                            ? null
                            : Path.GetFullPath(Path.Combine(_buildOptions.FallbackDocsetPath.Value, monodocConfig.SourceXmlFolder));
                        var fallbackOutputDirectory = _buildOptions.FallbackDocsetPath is null
                            ? null
                            : Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, ".fallback", monodocConfig.OutputYamlFolder));
                        ECMA2YamlConverter.Run(
                            xmlDirectory: Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, monodocConfig.SourceXmlFolder)),
                            outputDirectory: Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, monodocConfig.OutputYamlFolder)),
                            fallbackXmlDirectory: fallbackXmlPath,
                            fallbackOutputDirectory: fallbackOutputDirectory,
                            logWriter: LogError,
                            logContentBaseDirectory: _buildOptions.DocsetPath,
                            sourceMapFilePath: Path.Combine(_buildOptions.DocsetPath, $".sourcemap-{index}.json"),
                            config: monodocConfig);
                    }
                }
            }
        }

        private void LogError(LogItem item)
        {
            if (!string.IsNullOrEmpty(item.Code))
            {
                _errorLog.Write(
                    new Error(MapLevel(item.MessageSeverity), item.Code, item.Message, item.File is null ? null : new FilePath(item.File), item.Line ?? 0));
            }

            ErrorLevel MapLevel(MessageSeverity level)
                => level switch
                {
                    MessageSeverity.Error => ErrorLevel.Error,
                    MessageSeverity.Warning => ErrorLevel.Warning,
                    MessageSeverity.Suggestion => ErrorLevel.Suggestion,
                    MessageSeverity.Info => ErrorLevel.Info,
                    _ => ErrorLevel.Off,
                };
        }
    }
}
