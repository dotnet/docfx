// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.ConceptualDocuments;

[Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
class ValidateConceptualDocumentMetadata : BaseDocumentBuildStep
{
    private const string ConceptualKey = Constants.PropertyName.Conceptual;

    public override string Name => nameof(ValidateConceptualDocumentMetadata);

    public override int BuildOrder => 1;

    public override void Build(FileModel model, IHostService host)
    {
        if (model.Type != DocumentType.Article)
        {
            return;
        }
        if (!host.HasMetadataValidation)
        {
            return;
        }
        var metadata = ((Dictionary<string, object>)model.Content).ToImmutableDictionary().Remove(ConceptualKey);
        if (!model.Properties.IsUserDefinedTitle)
        {
            metadata = metadata.Remove(Constants.PropertyName.Title);
        }
        host.ValidateInputMetadata(model.OriginalFileAndType.File, metadata);
    }
}
