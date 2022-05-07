// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class RedirectionBuilder
{
    private readonly PublishModelBuilder _publishModelBuilder;
    private readonly RedirectionProvider _redirectionProvider;
    private readonly DocumentProvider _documentProvider;
    private readonly ContributionProvider _contributionProvider;

    public RedirectionBuilder(
        PublishModelBuilder publishModelBuilder,
        RedirectionProvider redirectionProvider,
        DocumentProvider documentProvider,
        ContributionProvider contributionProvider)
    {
        _publishModelBuilder = publishModelBuilder;
        _redirectionProvider = redirectionProvider;
        _documentProvider = documentProvider;
        _contributionProvider = contributionProvider;
    }

    internal void Build(ErrorBuilder errors, FilePath file)
    {
        var (redirectUrl, redirectConfigPath) = _redirectionProvider.GetRedirectUrl(errors, file);
        var (documentId, documentVersionIndependentId) = _documentProvider.GetDocumentId(_redirectionProvider.GetOriginalFile(file));

        var (_, sourceUrl, _) = _contributionProvider.GetGitUrl(new FilePath(redirectConfigPath));
        var publishMetadata = new JObject
        {
            ["redirect_url"] = redirectUrl,
            ["document_id"] = documentId,
            ["document_version_independent_id"] = documentVersionIndependentId,
            ["canonical_url"] = _documentProvider.GetCanonicalUrl(file),
            ["redirect_config_path"] = redirectConfigPath, // relative path to docset folder
            ["redirect_config_source_url"] = sourceUrl,
            ["redirect_source_path"] = file.Path.Value, // relative path to docset folder
        };

        _publishModelBuilder.AddOrUpdate(file, publishMetadata, outputPath: null);
    }
}
