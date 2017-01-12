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
            foreach (var item in from file in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                    from output in file.OutputFiles
                                    select new
                                    {
                                        IsHtml = output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase),
                                        output.Value.RelativePath,
                                        file.SourceRelativePath
                                    })
            {
                if (item.IsHtml)
                {
                    var outputFile = Path.Combine(outputFolder, item.RelativePath);
                    File.AppendAllText(outputFile, AppendString);
                }
                else
                {
                    Logger.LogWarning($"The output file {item.RelativePath} is not in html format.", file: item.SourceRelativePath);
                }
            }

            return manifest;
        }
    }
}
