// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;

namespace Docfx.Build.ManagedReference;

[Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class BuildManagedReferenceDocument : BuildReferenceDocumentBase
{
    public override string Name => nameof(BuildManagedReferenceDocument);

    protected override void BuildArticle(IHostService host, FileModel model)
    {
        var pageViewModel = (PageViewModel)model.Content;

        BuildArticleCore(host, model, shouldSkipMarkup: pageViewModel?.ShouldSkipMarkup ?? false);
    }
}
