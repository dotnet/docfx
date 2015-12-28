// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility.EntityMergers;

    [Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildManagedReferenceDocument : BaseDocumentBuildStep
    {
        private readonly ReflectionEntityMerger Merger = new ReflectionEntityMerger();

        public override string Name => nameof(BuildManagedReferenceDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    var page = (PageViewModel)model.Content;
                    foreach (var item in page.Items)
                    {
                        BuildItem(host, item, model);
                    }

                    model.File = Path.ChangeExtension(model.File, ".json");
                    break;
                case DocumentType.Override:
                    foreach (var item in (List<ItemViewModel>)model.Content)
                    {
                        BuildItem(host, item, model);
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        #region Private methods

        private void BuildItem(IHostService host, ItemViewModel item, FileModel model)
        {
            item.Summary = Markup(host, item.Summary, model);
            item.Remarks = Markup(host, item.Remarks, model);
            item.Conceptual = Markup(host, item.Conceptual, model);
            if (item.Syntax?.Return?.Description != null)
            {
                item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model);
            }
            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, model);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    exception.Description = Markup(host, exception.Description, model);
                }
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

        #endregion
    }
}
