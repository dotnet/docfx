// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.ResourceFiles;

[Export(nameof(ResourceDocumentProcessor), typeof(IDocumentBuildStep))]
public class ValidateResourceMetadata : BaseDocumentBuildStep
{
    public override string Name => nameof(ValidateResourceMetadata);

    public override int BuildOrder => 1;

    public override void Build(FileModel model, IHostService host)
    {
        if (!host.HasMetadataValidation)
        {
            return;
        }
        var metadata = (Dictionary<string, object>)model.Content;
        if (metadata != null)
        {
            host.ValidateInputMetadata(
                model.OriginalFileAndType.File,
                metadata.ToImmutableDictionary());
        }
    }
}
