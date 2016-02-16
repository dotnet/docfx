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
                    var restApi = (RestApiViewModel)model.Content;
                    var linkToUids = new HashSet<string>();
                    restApi.Summary = Markup(host, restApi.Summary, model);
                    restApi.Description = Markup(host, restApi.Description, model);
                    foreach (var item in restApi.Children)
                    {
                        item.Summary = Markup(host, item.Summary, model);
                        item.Description = Markup(host, item.Description, model);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
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
