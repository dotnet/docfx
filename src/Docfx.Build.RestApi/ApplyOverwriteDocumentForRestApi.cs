// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Docfx.Build.Common;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;

namespace Docfx.Build.RestApi;

[Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
public class ApplyOverwriteDocumentForRestApi : ApplyOverwriteDocument
{
    public override string Name => nameof(ApplyOverwriteDocumentForRestApi);

    public override int BuildOrder => 0x10;

    protected override IMerger GetMerger()
    {
        return new JObjectMerger(
            new JArrayMerger(
                base.GetMerger()));
    }

    public static IEnumerable<RestApiRootItemViewModel> GetRootItemsFromOverwriteDocument(FileModel overwriteModel, string uid, IHostService host)
    {
        return OverwriteDocumentReader.Transform<RestApiRootItemViewModel>(
            overwriteModel,
            uid,
            s => (RestApiRootItemViewModel)BuildRestApiDocument.BuildItem(host, s, overwriteModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
    }

    public static IEnumerable<RestApiRootItemViewModel> GetRootItemsToOverwrite(FileModel articleModel, string uid,
        IHostService host)
    {
        return new[] { (RestApiRootItemViewModel)articleModel.Content };
    }

    public static IEnumerable<RestApiChildItemViewModel> GetChildItemsFromOverwriteDocument(FileModel overwriteModel, string uid, IHostService host)
    {
        return OverwriteDocumentReader.Transform<RestApiChildItemViewModel>(
                overwriteModel,
                uid,
                s => (RestApiChildItemViewModel)BuildRestApiDocument.BuildItem(host, s, overwriteModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
    }

    public static IEnumerable<RestApiChildItemViewModel> GetChildItemsToOverwrite(FileModel articleModel, string uid, IHostService host)
    {
        return ((RestApiRootItemViewModel)articleModel.Content).Children.Where(c => c.Uid == uid);
    }

    public static IEnumerable<RestApiTagViewModel> GetTagsFromOverwriteDocument(FileModel overwriteModel, string uid, IHostService host)
    {
        return OverwriteDocumentReader.Transform<RestApiTagViewModel>(
            overwriteModel,
            uid,
            s => BuildRestApiDocument.BuildTag(host, s, overwriteModel, content => content != null && content.Trim() == Constants.ContentPlaceholder));
    }

    public static IEnumerable<RestApiTagViewModel> GetTagItemsToOverwrite(FileModel articleModel, string uid, IHostService host)
    {
        return ((RestApiRootItemViewModel)articleModel.Content).Tags.Where(c => c.Uid == uid);
    }

    protected override void ApplyOverwrite(IHostService host, List<FileModel> overwrites, string uid, List<FileModel> articles)
    {
        // 'articles' are filtered by registered uid, need further filtering by uid equality, then call getItemsToOverwrite function to select corresponding items
        var matchedArticles = articles.Where(a => uid == ((RestApiRootItemViewModel)a.Content).Uid).ToList();
        if (matchedArticles.Count > 0)
        {
            ApplyOverwrite(host, overwrites, uid, matchedArticles, GetRootItemsFromOverwriteDocument, GetRootItemsToOverwrite);
            return;
        }

        matchedArticles = articles.Where(a => ((RestApiRootItemViewModel)a.Content).Children.Any(c => uid == c.Uid)).ToList();
        if (matchedArticles.Count > 0)
        {
            ApplyOverwrite(host, overwrites, uid, matchedArticles, GetChildItemsFromOverwriteDocument, GetChildItemsToOverwrite);
            return;
        }

        matchedArticles = articles.Where(a => ((RestApiRootItemViewModel)a.Content).Tags.Any(t => uid == t.Uid)).ToList();
        if (matchedArticles.Count > 0)
        {
            ApplyOverwrite(host, overwrites, uid, matchedArticles, GetTagsFromOverwriteDocument, GetTagItemsToOverwrite);
            return;
        }
    }
}
