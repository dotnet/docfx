// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;

    internal sealed class HtmlPostProcessor : IPostProcessor
    {
        public List<IHtmlDocumentHandler> Handlers { get; } = new List<IHtmlDocumentHandler>();

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            foreach (var tuple in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                                  from output in item.OutputFiles
                                  where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                  select new
                                  {
                                      RelativePath = output.Value.RelativePath,
                                      SrcRelativePath = item.SourceRelativePath,
                                      Item = item,
                                  })
            {
                var filePath = Path.Combine(outputFolder, tuple.RelativePath);
                var document = new HtmlDocument();

                if (File.Exists(filePath))
                {
                    try
                    {
                        document.Load(filePath, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Warning: Can't load content from {filePath}: {ex.Message}");
                        continue;
                    }
                    foreach (var handler in Handlers)
                    {
                        handler.Handle(document, tuple.Item, tuple.SrcRelativePath, tuple.RelativePath);
                    }
                    document.Save(filePath, Encoding.UTF8);
                }
            }
            foreach (var handler in Handlers)
            {
                manifest = handler.Complete(manifest);
            }
            return manifest;
        }
    }
}
