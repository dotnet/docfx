namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Utility;
    using System.Linq;

    /// <summary>
    /// TODO: looks like it is duplicate work for each markdown file to load index file every time
    /// </summary>
    public class LoadApiIndex : IIndexerPipeline
    {

        private ApiReferenceModel _model = new ApiReferenceModel();

        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            var apiIndices = context.ApiIndexFiles;

            // Read index
            if (apiIndices == null || apiIndices.Length == 0)
            {
                return new ParseResult(ResultLevel.Success);
            }

            foreach (var index in apiIndices)
            {
                AddApiIndex(index);
            }

            context.ExternalApiIndex = _model;
            return new ParseResult(ResultLevel.Success);
        }

        private void AddApiIndex(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                ApiReferenceViewModel apiReferenceViewModel;
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        apiReferenceViewModel = YamlUtility.Deserialize<ApiReferenceViewModel>(sr);
                        AddToModel(apiReferenceViewModel, path);
                    }
                }
                catch (Exception e)
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, "{0} is not a valid API reference file, ignored: {1}", path, e.Message);
                }
            }
        }

        private void AddToModel(ApiReferenceViewModel viewModel, string path)
        {
            foreach(var i in viewModel)
            {
                ApiIndexItemModel item = new ApiIndexItemModel
                {
                    IndexFilePath = path,
                    Href = i.Value,
                    Name = i.Key,
                };

                // Override existing one
                _model[item.Name] = item;
            }
        }
    }
}
