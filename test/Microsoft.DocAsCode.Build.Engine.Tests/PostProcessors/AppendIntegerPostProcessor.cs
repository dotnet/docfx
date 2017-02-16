// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class AppendIntegerPostProcessor : IPostProcessor, ISupportIncrementalPostProcessor
    {
        public static readonly string HashValue = 1024.ToString();

        public static readonly string AppendInteger = $"-{HashValue}";

        public IPostProcessorHost PostProcessorHost { get; set; }

        public string GetIncrementalContextHash()
        {
            return HashValue;
        }

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            List<string> htmlList = new List<string>();
            var stream = PostProcessorHost.LoadContextInfo();
            if (stream != null)
            {
                using (var sw = new StreamReader(stream))
                {
                    htmlList = JsonUtility.Deserialize<List<string>>(sw);
                }
            }

            foreach (var relPath in from file in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                    from output in file.OutputFiles
                                    where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                    select output.Value.RelativePath)
            {
                EnvironmentContext.FileAbstractLayer.WriteAllText(
                    relPath,
                    EnvironmentContext.FileAbstractLayer.ReadAllText(relPath) + AppendInteger);
                if (!htmlList.Contains(relPath))
                {
                    htmlList.Add(relPath);
                }
            }

            using (var saveStream = PostProcessorHost.SaveContextInfo())
            using (var sw = new StreamWriter(saveStream))
            {
                JsonUtility.Serialize(sw, htmlList);
            }

            return manifest;
        }
    }
}
