// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class LegacyFileMap
    {
        public static void Convert(Docset docset, Context context, List<Document> documents, Dictionary<string, List<LegacyDependencyMapItem>> dependencyMap)
        {
            using (Progress.Start("Convert Legacy File Map"))
            {
                var listBuilder = new ListBuilder<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)>();
                Parallel.ForEach(
                    documents,
                    document =>
                    {
                        if (document.IsSchemaData)
                        {
                            return;
                        }
                        var legacyOutputFilePathRelativeToSiteBasePath = document.ToLegacyOutputPathRelativeToBaseSitePath(docset);

                        var fileItem = LegacyFileMapItem.Instance(legacyOutputFilePathRelativeToSiteBasePath, document.ContentType);
                        if (fileItem != null)
                        {
                            listBuilder.Add((document.ToLegacyPathRelativeToBasePath(docset), fileItem));
                        }
                    });

                var fileMapItems = listBuilder.ToList();
                Convert(docset, context, fileMapItems);
                LegacyAggregatedFileMap.Convert(docset, context, fileMapItems, dependencyMap);
            }
        }

        public static void Convert(Docset docset, Context context, IEnumerable<(string legacyFilePathRelativeToBaseFolder, LegacyFileMapItem fileMapItem)> items)
        {
            context.Output.WriteJson(
                new
                {
                    host = docset.Config.BaseUrl,
                    locale = docset.Locale,
                    base_path = $"/{docset.Config.DocumentId.SiteBasePath}",
                    source_base_path = docset.Config.DocumentId.SourceBasePath,
                    version_info = new { },
                    file_mapping = items.ToDictionary(
                        key => PathUtility.NormalizeFile(key.legacyFilePathRelativeToBaseFolder), v => v.fileMapItem),
                },
                Path.Combine(docset.Config.DocumentId.SiteBasePath, "filemap.json"));
        }
    }
}
