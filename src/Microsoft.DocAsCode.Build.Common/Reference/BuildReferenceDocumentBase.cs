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
        private readonly IModelAttributeHandler _defaultHandler =
            new CompositeModelAttributeHandler(
                new UniqueIdentityReferenceHandler(),
                new MarkdownContentHandler()
            );

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
            // overwrite model is now shared
        }

        protected virtual void BuildArticleCore(IHostService host, FileModel model, IModelAttributeHandler handlers = null, HandleModelAttributesContext handlerContext = null, bool shouldSkipMarkup = false)
        {
            if (handlers == null)
            {
                handlers = _defaultHandler;
            }
            if (handlerContext == null)
            {
                handlerContext = new HandleModelAttributesContext
                {
                    EnableContentPlaceholder = false,
                    Host = host,
                    FileAndType = model.OriginalFileAndType,
                    SkipMarkup = shouldSkipMarkup,
                };
            }

            handlers.Handle(model.Content, handlerContext);

            model.LinkToUids = model.LinkToUids.Union(handlerContext.LinkToUids);
            model.LinkToFiles = model.LinkToFiles.Union(handlerContext.LinkToFiles);
            model.FileLinkSources = model.FileLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(handlerContext.FileLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
            model.UidLinkSources = model.UidLinkSources.ToDictionary(v => v.Key, v => v.Value.ToList())
                .Merge(handlerContext.UidLinkSources.Select(i => new KeyValuePair<string, IEnumerable<LinkSourceInfo>>(i.Key, i.Value)))
                .ToImmutableDictionary(v => v.Key, v => v.Value.ToImmutableList());
            foreach (var d in handlerContext.Dependency)
            {
                host.ReportDependencyTo(model, d, DependencyTypeName.Include);
            }
        }
    }
}
