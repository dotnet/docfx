// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.UniversalReference;
using Docfx.Plugins;

namespace Docfx.Build.UniversalReference;

[Export(nameof(UniversalReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class BuildUniversalReferenceDocument : BuildReferenceDocumentBase
{
    public override string Name => nameof(BuildUniversalReferenceDocument);

    #region BuildReferenceDocumentBase

    protected override void BuildArticle(IHostService host, FileModel model)
    {
        var pageViewModel = (PageViewModel)model.Content;

        BuildArticleCore(host, model, shouldSkipMarkup: pageViewModel?.ShouldSkipMarkup ?? false);
    }

    #endregion
}
