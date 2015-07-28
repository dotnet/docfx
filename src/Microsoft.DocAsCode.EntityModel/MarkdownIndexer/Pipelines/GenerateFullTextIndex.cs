// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class GenerateFullTextIndex : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            return new ParseResult(ResultLevel.Success);
        }
    }
}
