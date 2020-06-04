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
                    if (Directory.Exists(_config.Monodoc.OutputYamlFolder))
                    {
                        Directory.Delete(_config.Monodoc.OutputYamlFolder, recursive: true);
                    }

                    Directory.CreateDirectory(_config.Monodoc.OutputYamlFolder);

                    var fallbackXmlPath = _buildOptions.FallbackDocsetPath is null
                        ? null
                        : Path.Combine(_buildOptions.FallbackDocsetPath.Value, _config.Monodoc.SourceXmlFolder);
                    var fallbackOutputDirectory = _buildOptions.FallbackDocsetPath is null
                        ? null
                        : Path.Combine(_buildOptions.DocsetPath, ".fallback", _config.Monodoc.OutputYamlFolder);
                    ECMA2YamlConverter.Run(
                        xmlDirectory: Path.Combine(_buildOptions.DocsetPath, _config.Monodoc.SourceXmlFolder),
                        outputDirectory: Path.Combine(_buildOptions.DocsetPath, _config.Monodoc.OutputYamlFolder),
                        fallbackXmlDirectory: fallbackXmlPath,
                        fallbackOutputDirectory: fallbackOutputDirectory,
                        logWriter: LogError,
                        logContentBaseDirectory: _buildOptions.DocsetPath,
                        sourceMapFilePath: Path.Combine(_buildOptions.DocsetPath, ".sourcemap.json"),
                        config: _config.Monodoc);
                }
            }
        }

        private void LogError(LogItem item)
        {
            if (!string.IsNullOrEmpty(item.Code))
            {
                _errorLog.Write(new Error(MapLevel(item.MessageSeverity), item.Code, item.Message, new FilePath(item.File), item.Line ?? 0));
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
