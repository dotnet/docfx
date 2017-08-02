// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(JavaScriptReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildJavaScriptReferenceDocument : BuildReferenceDocumentBase
    {
        public override string Name => nameof(BuildJavaScriptReferenceDocument);

        #region BuildReferenceDocumentBase

        protected override void BuildArticle(IHostService host, FileModel model)
        {
            var page = (PageViewModel)model.Content;
            foreach (var item in page.Items)
            {
                BuildItem(host, item, model);
            }
        }

        #endregion

        public static ItemViewModel BuildItem(IHostService host, ItemViewModel item, FileModel model, Func<string, bool> filter = null)
        {
            var linkToUids = new HashSet<string>();
            var pageViewModel = model.Content as PageViewModel;
            var skip = pageViewModel?.ShouldSkipMarkup;

            if (skip != true)
            {
                item.Summary = Markup(host, item.Summary, model, filter);
                item.Remarks = Markup(host, item.Remarks, model, filter);
                if (model.Type != DocumentType.Overwrite)
                {
                    item.Conceptual = Markup(host, item.Conceptual, model, filter);
                }
            }

            linkToUids.UnionWith(item.Inheritance ?? EmptyEnumerable);
            linkToUids.UnionWith(item.DerivedClasses ?? EmptyEnumerable);
            linkToUids.UnionWith(item.InheritedMembers ?? EmptyEnumerable);
            linkToUids.UnionWith(item.Implements ?? EmptyEnumerable);
            linkToUids.UnionWith(item.SeeAlsos?.Where(s => s.LinkType == LinkType.CRef)?.Select(s => s.LinkId) ?? EmptyEnumerable);
            linkToUids.UnionWith(item.Sees?.Where(s => s.LinkType == LinkType.CRef)?.Select(s => s.LinkId) ?? EmptyEnumerable);

            if (item.Overridden != null)
            {
                linkToUids.Add(item.Overridden);
            }

            if (item.Syntax?.Return != null)
            {
                if (item.Syntax.Return.Description != null && skip != true)
                {
                    item.Syntax.Return.Description = Markup(host, item.Syntax?.Return?.Description, model, filter);
                }
                linkToUids.UnionWith(item.Syntax.Return.Type ?? EmptyEnumerable);
            }

            var parameters = item.Syntax?.Parameters;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (skip != true)
                    {
                        parameter.Description = Markup(host, parameter.Description, model, filter);
                    }
                    linkToUids.UnionWith(parameter.Type ?? EmptyEnumerable);
                }
            }
            if (item.Exceptions != null)
            {
                foreach (var exception in item.Exceptions)
                {
                    if (skip != true)
                    {
                        exception.Description = Markup(host, exception.Description, model, filter);
                    }
                    linkToUids.Add(exception.Type);
                }
            }

            model.LinkToUids = model.LinkToUids.Union(linkToUids);
            return item;
        }

        #region Private methods
        private static readonly IEnumerable<string> EmptyEnumerable = Enumerable.Empty<string>();

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

            var mr = host.Markup(markdown, model.OriginalFileAndType);
            model.LinkToFiles = model.LinkToFiles.Union(mr.LinkToFiles);
            model.LinkToUids = model.LinkToUids.Union(mr.LinkToUids);

            var fls = model.FileLinkSources.ToDictionary(p => p.Key, p => p.Value);
            foreach (var pair in mr.FileLinkSources)
            {
                if (fls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
                {
                    fls[pair.Key] = list.AddRange(pair.Value);
                }
                else
                {
                    fls[pair.Key] = pair.Value;
                }
            }
            model.FileLinkSources = fls.ToImmutableDictionary();

            var uls = model.UidLinkSources.ToDictionary(p => p.Key, p => p.Value);
            foreach (var pair in mr.UidLinkSources)
            {
                if (uls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
                {
                    uls[pair.Key] = list.AddRange(pair.Value);
                }
                else
                {
                    uls[pair.Key] = pair.Value;
                }
            }
            model.UidLinkSources = uls.ToImmutableDictionary();

            return mr.Html;
        }

        #endregion
    }
}
