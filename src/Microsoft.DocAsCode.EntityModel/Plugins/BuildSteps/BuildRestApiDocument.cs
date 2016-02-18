// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildRestApiDocument : BaseDocumentBuildStep
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public override string Name => nameof(BuildRestApiDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    var restApi = (RestApiItemViewModel)model.Content;
                    BuildItem(host, restApi, model);
                    if (restApi.Children != null)
                    {
                        foreach (var item in restApi.Children)
                        {
                            BuildItem(host, item, model);
                        }
                    }
                    break;
                case DocumentType.Override:
                    foreach (var item in (List<RestApiItemViewModel>)model.Content)
                    {
                        BuildItem(host, item, model);
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        private void BuildItem(IHostService host, RestApiItemViewModel item, FileModel model)
        {
            item.Summary = Markup(host, item.Summary, model);
            item.Description = Markup(host, item.Description, model);
            item.Conceptual = Markup(host, item.Conceptual, model);
        }

        private string Markup(IHostService host, string markdown, FileModel model)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }
            var mr = host.Markup(markdown, model.FileAndType);
            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(mr.LinkToFiles);
            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(mr.LinkToUids);
            return mr.Html;
        }
    }
}
