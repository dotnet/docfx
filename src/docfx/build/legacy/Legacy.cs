// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(Docset docset, Context context, IEnumerable<Document> documents)
        {
            var fileMapItems = new ConcurrentBag<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();

            Parallel.ForEach(documents, document =>
            {
                var outputFileName = Path.GetFileName(document.OutputPath);
                var relativeOutputFilePath = document.OutputPath;
                var absoluteOutputFilePath = Path.Combine(docset.Config.Output.Path, relativeOutputFilePath);
                var legacyOutputFilePathRelativeToSiteBasePath = relativeOutputFilePath;
                if (relativeOutputFilePath.StartsWith(docset.Config.SiteBasePath, StringComparison.Ordinal))
                {
                    legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, relativeOutputFilePath);
                }

                var fileItem = LegacyFileMapItem.Instance(legacyOutputFilePathRelativeToSiteBasePath, document.ContentType);
                if (fileItem != null)
                {
                    fileMapItems.Add((Path.GetRelativePath(docset.Config.SourceBasePath, document.FilePath), fileItem));
                }

                switch (document.ContentType)
                {
                    case ContentType.TableOfContents:
                        LegacyTableOfContents.Convert(docset, context, absoluteOutputFilePath, relativeOutputFilePath, legacyOutputFilePathRelativeToSiteBasePath);
                        break;

                    case ContentType.Markdown:
                        LegacyMarkdown.Convert(docset, context, absoluteOutputFilePath, relativeOutputFilePath, legacyOutputFilePathRelativeToSiteBasePath);
                        break;
                }
            });

            LegacyFileMap.Convert(docset, context, fileMapItems);
        }

        private static void ConvertMarkdownModel(Docset docset, Context context, string absoluteOutputFilePath)
        {

        }
    }
}
