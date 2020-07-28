// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static void Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var errors = context.ErrorBuilder;
            var redirectUrl = context.RedirectionProvider.GetRedirectUrl(errors, file.FilePath);
            var (documentId, documentVersionIndependentId) = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file.FilePath));

            var publishMetadata = new JObject
            {
                ["redirect_url"] = redirectUrl,
                ["document_id"] = documentId,
                ["document_version_independent_id"] = documentVersionIndependentId,
                ["canonical_url"] = file.CanonicalUrl,
            };

            context.PublishModelBuilder.SetPublishItem(file.FilePath, publishMetadata, outputPath: null);
        }
    }
}
