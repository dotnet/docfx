// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Composition;

using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.DataContracts.UniversalReference;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.UniversalReference;

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
