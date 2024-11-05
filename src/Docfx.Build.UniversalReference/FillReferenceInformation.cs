// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.UniversalReference;
using Docfx.Plugins;

namespace Docfx.Build.UniversalReference;

[Export(nameof(UniversalReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class FillReferenceInformation : BaseDocumentBuildStep
{
    public override string Name => nameof(FillReferenceInformation);

    public override int BuildOrder => 0x20;

    public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        foreach (var model in models)
        {
            if (model.Type != DocumentType.Article)
            {
                continue;
            }

            FillCore((PageViewModel)model.Content, host);
        }
    }

    #region Private methods

    private static void FillCore(PageViewModel model, IHostService host)
    {
        if (model.References == null || model.References.Count == 0)
        {
            return;
        }
        foreach (var r in model.References)
        {
            var m = host.LookupByUid(r.Uid).Find(x => x.Type == DocumentType.Article);
            if (m == null)
            {
                continue;
            }
            var page = (PageViewModel)m.Content;
            var item = page.Items.Find(x => x.Uid == r.Uid);
            if (item == null)
            {
                continue;
            }
            FillContent(r, item);
        }
    }

    private static void FillContent(ReferenceViewModel r, dynamic item)
    {
        if (item.Metadata != null)
        {
            foreach (var pair in item.Metadata)
            {
                switch (pair.Key)
                {
                    case Constants.ExtensionMemberPrefix.Spec:
                        break;
                    default:
                        r.Additional[pair.Key] = pair.Value;
                        break;
                }
            }
        }

        r.Additional[Constants.PropertyName.Summary] = item.Summary;
        r.Additional[Constants.PropertyName.Type] = item.Type;
        r.Additional[Constants.PropertyName.Syntax] = item.Syntax;
        r.Additional[Constants.PropertyName.Platform] = item.Platform;
    }
    #endregion
}
