// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static void Build(Context context, FilePath file)
        {
            var errors = context.ErrorBuilder;
            var redirectUrl = context.RedirectionProvider.GetRedirectUrl(errors, file);
            var (documentId, documentVersionIndependentId) = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file));

            var publishMetadata = new JObject
            {
                ["redirect_url"] = redirectUrl,
                ["document_id"] = documentId,
                ["document_version_independent_id"] = documentVersionIndependentId,
                ["canonical_url"] = context.DocumentProvider.GetCanonicalUrl(file),
            };

            context.PublishModelBuilder.SetPublishItem(file, publishMetadata, outputPath: null);
        }
    }
}
