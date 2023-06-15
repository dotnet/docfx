// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;

using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.ManagedReference;

[Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class ApplyOverwriteDocumentForMref : ApplyOverwriteDocument
{
    public override string Name => nameof(ApplyOverwriteDocumentForMref);

    public override int BuildOrder => 0x10;

    public IEnumerable<ItemViewModel> GetItemsFromOverwriteDocument(FileModel fileModel, string uid, IHostService host)
    {
        return Transform<ItemViewModel>(fileModel, uid, host);
    }

    public IEnumerable<ItemViewModel> GetItemsToOverwrite(FileModel fileModel, string uid, IHostService host)
    {
        return ((PageViewModel)fileModel.Content).Items.Where(s => s.Uid == uid);
    }

    protected override void ApplyOverwrite(IHostService host, List<FileModel> overwrites, string uid, List<FileModel> articles)
    {
        ApplyOverwrite(host, overwrites, uid, articles, GetItemsFromOverwriteDocument, GetItemsToOverwrite);
    }
}
