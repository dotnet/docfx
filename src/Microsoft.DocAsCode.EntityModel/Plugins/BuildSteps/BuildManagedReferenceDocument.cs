// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
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
                case DocumentType.Overwrite:
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
        private static IEnumerable<string> EmptyEnumerable = Enumerable.Empty<string>();
        private void BuildItem(IHostService host, ItemViewModel item, FileModel model)
        {
            var linkToUids = new HashSet<string>();
            item.Summary = Markup(host, item.Summary, model);
            item.Remarks = Markup(host, item.Remarks, model);
            item.Conceptual = Markup(host, item.Conceptual, model);
            linkToUids.UnionWith(item.Inheritance ?? EmptyEnumerable);
            linkToUids.UnionWith(item.InheritedMembers ?? EmptyEnumerable);
            linkToUids.UnionWith(item.Implements ?? EmptyEnumerable);
            linkToUids.UnionWith(item.SeeAlsos?.Select(s => s.Type) ?? EmptyEnumerable);
            linkToUids.UnionWith(item.Sees?.Select(s => s.Type) ?? EmptyEnumerable);

            if (item.Overridden != null)
            {
                linkToUids.Add(item.Overridden);
            }
            
            if (item.Syntax?.Return != null)
            {
                if (item.Syntax.Return.Description != null)
                {
                    item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model);
                }

                linkToUids.Add(item.Syntax.Return.Type);
            }

            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, model);
                    linkToUids.Add(parameter.Type);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    exception.Description = Markup(host, exception.Description, model);
                    linkToUids.Add(exception.Type);
                }
            }

            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(linkToUids);
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
