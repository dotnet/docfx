// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;

namespace Docfx.Build.RestApi;

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
                    // use RestApiChildItemViewModel because it contains all properties for REST
                    var metadata = item.ConvertTo<RestApiChildItemViewModel>().Metadata.ToImmutableDictionary();
                    host.ValidateInputMetadata(model.OriginalFileAndType.File, metadata);
                }
                break;
            default:
                throw new NotSupportedException();
        }
    }
}
