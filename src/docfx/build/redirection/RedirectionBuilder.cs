// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class RedirectionBuilder
{
    private readonly PublishModelBuilder _publishModelBuilder;
    private readonly RedirectionProvider _redirectionProvider;
    private readonly DocumentProvider _documentProvider;

    public RedirectionBuilder(PublishModelBuilder publishModelBuilder, RedirectionProvider redirectionProvider, DocumentProvider documentProvider)
    {
        _publishModelBuilder = publishModelBuilder;
        _redirectionProvider = redirectionProvider;
        _documentProvider = documentProvider;
    }

    internal void Build(ErrorBuilder errors, FilePath file)
    {
        var redirectUrl = _redirectionProvider.GetRedirectUrl(errors, file);
        var (documentId, documentVersionIndependentId) = _documentProvider.GetDocumentId(_redirectionProvider.GetOriginalFile(file));

        var publishMetadata = new JObject
        {
            ["redirect_url"] = redirectUrl,
            ["document_id"] = documentId,
            ["document_version_independent_id"] = documentVersionIndependentId,
            ["canonical_url"] = _documentProvider.GetCanonicalUrl(file),
        };

        _publishModelBuilder.AddOrUpdate(file, publishMetadata, outputPath: null);
    }
}
