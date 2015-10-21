// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public static class MarkdownIndexer
    {
        // Order matters
        static List<IIndexerPipeline> pipelines = new List<IIndexerPipeline>()
        {
            new LoadApiIndex(),
            new LoadMarkdownFile(),
            new ResolveApiReference(),
            new ResolveCodeSnippet(),
            new GenerateFullTextIndex(), // TODO: Ignore the text if it contains YAML HEADER?
            new ResolveYamlHeader(),
            new Save(),
        };

        /// <summary>
        /// Save to **.md.map
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ParseResult Exec(IndexerContext context)
        {
            MapFileItemViewModel viewModel = new MapFileItemViewModel();
            return ExecutePipeline(viewModel, context);
        }

        private static ParseResult ExecutePipeline(MapFileItemViewModel index, IndexerContext context)
        {
            ParseResult result = new ParseResult(ResultLevel.Success);
            foreach (var pipeline in pipelines)
            {
                try
                {
                    result = pipeline.Run(index, context);
                    if (result.ResultLevel == ResultLevel.Error)
                    {
                        return result;
                    }

                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        Logger.Log(result);
                    }
                }
                catch (Exception e)
                {
                    return new ParseResult(ResultLevel.Warning, "Issue encountered when processing markdown file {0}, {1}.", context.MarkdownFileSourcePath, e.Message);
                }
            }

            return result;
        }
    }
}
