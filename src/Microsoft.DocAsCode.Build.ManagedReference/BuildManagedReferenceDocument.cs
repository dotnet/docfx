// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
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
                    break;
                case DocumentType.Overwrite:
                    BuildItem(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ItemViewModel BuildItem(IHostService host, ItemViewModel item, FileModel model, Func<string, bool> filter = null)
        {
            var linkToUids = new HashSet<string>();
            item.Summary = Markup(host, item.Summary, model, filter);
            item.Remarks = Markup(host, item.Remarks, model, filter);
            if (model.Type != DocumentType.Overwrite)
            {
                item.Conceptual = Markup(host, item.Conceptual, model, filter);
            }
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
                    item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model, filter);
                }

                linkToUids.Add(item.Syntax.Return.Type);
            }

            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Description = Markup(host, parameter.Description, model, filter);
                    linkToUids.Add(parameter.Type);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    exception.Description = Markup(host, exception.Description, model, filter);
                    linkToUids.Add(exception.Type);
                }
            }

            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(linkToUids);
            return item;
        }

        #region Private methods
        private static IEnumerable<string> EmptyEnumerable = Enumerable.Empty<string>();

        private static void BuildItem(IHostService host, FileModel model)
        {
            var file = model.FileAndType;
            var overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
            model.Content = overwrites;
            model.LocalPathFromRepoRoot = overwrites[0].Documentation?.Remote?.RelativePath ?? Path.Combine(file.BaseDir, file.File).ToDisplayPath();
            model.Uids = (from item in overwrites
                          select new UidDefinition(
                              item.Uid,
                              model.LocalPathFromRepoRoot,
                              item.Documentation.StartLine + 1)).ToImmutableArray();
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

        #endregion
    }
}
