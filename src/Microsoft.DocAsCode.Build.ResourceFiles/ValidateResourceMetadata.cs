// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Composition;
using System.Collections.Immutable;

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
