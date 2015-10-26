// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;

    public class ResolveApiReference : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            var filePath = context.MarkdownFileTargetPath;
            var apis = context.ExternalApiIndex;
            var externalReferences = context.ExternalReferences;
            var content = context.MarkdownContent;
            var links = LinkParser.Select(content);
            if (links == null || links.Count == 0) return new ParseResult(ResultLevel.Info, "No Api reference found for {0}", filePath);
            if (item.References == null) item.References = new ReferencesViewModel();
            ReferencesViewModel references = item.References;

            foreach (var matchDetail in links)
            {
                var referenceId = matchDetail.Id;
                var apiId = matchDetail.Id;

                // TODO: Support short name resolve 
                string apiHref = GetApiHref(apis, externalReferences, apiId, filePath);
                if (!string.IsNullOrEmpty(apiHref))
                {
                    var reference = new MapFileItemViewModel
                    {
                        Id = referenceId,
                        ReferenceKeys = matchDetail.MatchedSections,
                        Href = apiHref,
                        MapFileType = MapFileType.Link
                    };

                    // Api Index file only contains Id and Href
                    references.AddItem(reference);
                }
            }

            return new ParseResult(ResultLevel.Success);
        }

        private string GetApiHref(ApiReferenceModel dict, IEnumerable<ExternalReferencePackageReader> externalReferences, string key, string currentPath)
        {
            ApiIndexItemModel api;
            // 1. Try resolve with full name
            if (dict.TryGetValue(key, out api))
            {
                var indexFolder = Path.GetDirectoryName(api.IndexFilePath);
                var apiYamlFilePath = FileExtensions.GetFullPath(indexFolder, api.Href);
                return FileExtensions.MakeRelativePath(Path.GetDirectoryName(currentPath), apiYamlFilePath);
            }

            // 2. Try resolve external references with full name
            foreach (var externalReference in externalReferences)
            {
                ReferenceViewModel vm;
                if (externalReference.TryGetReference(key, out vm))
                {
                    return vm.Href;
                }
            }

            return null;
        }

    }
}
