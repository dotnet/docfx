// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Collections.Immutable;

using Microsoft.DocAsCode.Build.Common;
using Microsoft.DocAsCode.DataContracts.RestApi;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.RestApi;

[Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
public class ValidateRestApiDocumentMetadata : BaseDocumentBuildStep
{
    public override string Name => nameof(ValidateRestApiDocumentMetadata);

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
                    host.ValidateInputMetadata(
                        model.OriginalFileAndType.File,
                        // use RestApiChildItemViewModel because it contains all properties for REST.
                        item.ConvertTo<RestApiChildItemViewModel>().Metadata.ToImmutableDictionary());
                }
                break;
            default:
                throw new NotSupportedException();
        }
    }
}
