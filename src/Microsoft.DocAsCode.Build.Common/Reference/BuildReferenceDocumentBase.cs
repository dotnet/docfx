// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// Base document build step for reference.
    /// </summary>
    public abstract class BuildReferenceDocumentBase : BaseDocumentBuildStep
    {
        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
                    BuildArticle(host, model);
                    break;
                case DocumentType.Overwrite:
                    BuildOverwrite(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected abstract void BuildArticle(IHostService host, FileModel model);

        protected virtual void BuildOverwrite(IHostService host, FileModel model)
        {
            var overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
            model.Content = overwrites;
            model.LinkToFiles = overwrites.SelectMany(o => o.LinkToFiles).ToImmutableHashSet();
            model.LinkToUids = overwrites.SelectMany(o => o.LinkToUids).ToImmutableHashSet();
            model.FileLinkSources = overwrites.SelectMany(o => o.FileLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
            model.UidLinkSources = overwrites.SelectMany(o => o.UidLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
            model.Uids = (from item in overwrites
                          select new UidDefinition(
                              item.Uid,
                              model.LocalPathFromRoot,
                              item.Documentation.StartLine + 1)).ToImmutableArray();
        }

        protected virtual void HandleAttributes<T>(FileModel model, IModelAttributeHandler handlers, HandleModelAttributesContext handlerContext)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }
            if (handlerContext == null)
            {
                throw new ArgumentNullException(nameof(handlerContext));
            }
            if (!(model.Content is T))
            {
                throw new InvalidCastException($"Content of the model '{model.LocalPathFromRoot}' should be type of {typeof(T)}.");
            }

            var modelContent = (T)model.Content;

            handlers.Handle(modelContent, handlerContext);

            model.LinkToUids = model.LinkToUids.Union(handlerContext.LinkToUids);
            model.LinkToFiles = model.LinkToFiles.Union(handlerContext.LinkToFiles);
            model.FileLinkSources = model.FileLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(handlerContext.FileLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
            model.UidLinkSources = model.UidLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(handlerContext.UidLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
        }
    }
}
