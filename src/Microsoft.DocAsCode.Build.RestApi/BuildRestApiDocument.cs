// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Composition;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.RestApi.ViewModels;
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
                    var restApi = (RestApiRootItemViewModel)model.Content;
                    BuildItem(host, restApi, model);
                    if (restApi.Children != null)
                    {
                        foreach (var item in restApi.Children)
                        {
                            BuildItem(host, item, model);
                        }
                    }
                    if (restApi.Tags != null)
                    {
                        foreach (var tag in restApi.Tags)
                        {
                            tag.Description = Markup(host, tag.Description, model);
                        }
                    }
                    break;
                case DocumentType.Overwrite:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static RestApiItemViewModelBase BuildItem(IHostService host, RestApiItemViewModelBase item, FileModel model, Func<string, bool> filter = null)
        {
            item.Summary = Markup(host, item.Summary, model, filter);
            item.Description = Markup(host, item.Description, model, filter);
            item.Conceptual = Markup(host, item.Conceptual, model, filter);
            return item;
        }

        private static string Markup(IHostService host, string markdown, FileModel model, Func<string, bool> filter = null)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            if (filter != null && filter(markdown))
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
