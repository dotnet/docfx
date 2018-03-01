// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class AppendStringPostProcessor : IPostProcessor, ISupportIncrementalPostProcessor
    {
        public const string AppendString = " is processed";
        public const string AdditionalExtensionString = ".html.additional";

        public IPostProcessorHost PostProcessorHost { get; set; }

        public string GetIncrementalContextHash()
        {
            return null;
        }

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            foreach (var file in manifest.Files ?? Enumerable.Empty<ManifestItem>())
            {
                string htmlRelativePath = null;
                foreach (var outputFile in file.OutputFiles)
                {
                    if (outputFile.Key.Equals(".html", StringComparison.OrdinalIgnoreCase))
                    {
                        htmlRelativePath = outputFile.Value.RelativePath;
                        EnvironmentContext.FileAbstractLayer.WriteAllText(
                            htmlRelativePath,
                            EnvironmentContext.FileAbstractLayer.ReadAllText(htmlRelativePath) + AppendString);
                    }
                    else
                    {
                        Logger.LogWarning($"The output file {outputFile.Value.RelativePath} is not in html format.", file: file.SourceRelativePath);
                    }
                }

                // Add additional html output file
                if (htmlRelativePath != null)
                {
                    var targetRelativePath = Path.ChangeExtension(htmlRelativePath, AdditionalExtensionString);
                    file.OutputFiles.Add(AdditionalExtensionString,
                        new OutputFileInfo
                        {
                            RelativePath = targetRelativePath
                        });
                    EnvironmentContext.FileAbstractLayer.Copy(htmlRelativePath, targetRelativePath);
                }
            }

            return manifest;
        }
    }
}
