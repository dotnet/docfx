// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;

namespace Docfx.Build.ManagedReference;

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
