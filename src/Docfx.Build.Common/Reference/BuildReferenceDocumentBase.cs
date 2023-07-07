// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Common;

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
        var overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
        model.Content = overwrites;
        model.LinkToFiles = overwrites.SelectMany(o => o.LinkToFiles).ToImmutableHashSet();
        model.LinkToUids = overwrites.SelectMany(o => o.LinkToUids).ToImmutableHashSet();
        model.FileLinkSources = overwrites.SelectMany(o => o.FileLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
        model.UidLinkSources = overwrites.SelectMany(o => o.UidLinkSources).GroupBy(i => i.Key, i => i.Value).ToImmutableDictionary(i => i.Key, i => i.SelectMany(l => l).ToImmutableList());
        model.Uids = (from item in overwrites
                      where !string.IsNullOrEmpty(item.Uid)
                      select new UidDefinition(
                          item.Uid,
                          model.LocalPathFromRoot,
                          item.Documentation.StartLine + 1)).ToImmutableArray();
    }

    protected virtual void BuildArticleCore(IHostService host, FileModel model, IModelAttributeHandler handlers = null, HandleModelAttributesContext handlerContext = null, bool shouldSkipMarkup = false)
    {
        handlers ??= _defaultHandler;
        handlerContext ??= new HandleModelAttributesContext
        {
            EnableContentPlaceholder = false,
            Host = host,
            FileAndType = model.OriginalFileAndType,
            SkipMarkup = shouldSkipMarkup,
        };

        handlers.Handle(model.Content, handlerContext);

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
