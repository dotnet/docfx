// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Collections.Immutable;

using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.ManagedReference;

[Export(nameof(ManagedReferenceDocumentProcessor), typeof(IDocumentBuildStep))]
public class ValidateManagedReferenceDocumentMetadata : BaseDocumentBuildStep
{
    public override string Name => nameof(ValidateManagedReferenceDocumentMetadata);

    public override int BuildOrder => 1;

    public override void Build(FileModel model, IHostService host)
    {
        if (!host.HasMetadataValidation)
        {
            return;
        }
        switch (model.Type)
        {
            case DocumentType.Article:
                break;
            case DocumentType.Overwrite:
                foreach (var item in (List<OverwriteDocumentModel>)model.Content)
                {
                    host.ValidateInputMetadata(model.OriginalFileAndType.File, item.ConvertTo<ItemViewModel>().Metadata.ToImmutableDictionary());
                }
                break;
            default:
                throw new NotSupportedException();
        }
    }
}
