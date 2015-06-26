﻿namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
                ApiIndexItemModel api;
                if (apis.TryGetValue(apiId, out api))
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
    }
}
