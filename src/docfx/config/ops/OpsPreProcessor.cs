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

        public OpsPreProcessor(Config config, BuildOptions buildOptions)
        {
            _config = config;
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

                    ECMA2YamlConverter.Run(
                        Path.Combine(_buildOptions.DocsetPath, _config.Monodoc.SourceXmlFolder),
                        Path.Combine(_buildOptions.OutputPath, _config.Monodoc.OutputYamlFolder),
                        fallbackXmlPath,
                        null,
                        _buildOptions.DocsetPath,
                        Path.Combine(_buildOptions.DocsetPath, ".sourcemap.json"),
                        _config.Monodoc);
                }
            }
        }
    }
}
