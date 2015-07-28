// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;

    public class ResolveApiReference : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            var filePath = context.MarkdownFileTargetPath;
            var apis = context.ExternalApiIndex;
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
                ApiIndexItemModel api = GetApi(apis, apiId, null);
                if (api != null)
                {
                    var indexFolder = Path.GetDirectoryName(api.IndexFilePath);
                    var apiYamlFilePath = FileExtensions.GetFullPath(indexFolder, api.Href);
                    var reference = new MapFileItemViewModel
                    {
                        Id = referenceId,
                        ReferenceKeys = matchDetail.MatchedSections,
                        Href = FileExtensions.MakeRelativePath(Path.GetDirectoryName(filePath), apiYamlFilePath),
                        MapFileType = MapFileType.Link
                    };

                    // Api Index file only contains Id and Href
                    references.AddItem(reference);
                }
            }

            return new ParseResult(ResultLevel.Success);
        }

        private ApiIndexItemModel GetApi(ApiReferenceModel dict, string key, string currentNamespace)
        {
            ApiIndexItemModel api;
            // 1. Try resolve with full name
            if (dict.TryGetValue(key, out api))
            {
                return api;
            }

            if (string.IsNullOrEmpty(currentNamespace)) return null;

            // 2. Append current namespace
            key = currentNamespace + "." + key;
            if (dict.TryGetValue(key, out api))
            {
                return api;
            }
            return null;
        }

    }
}
