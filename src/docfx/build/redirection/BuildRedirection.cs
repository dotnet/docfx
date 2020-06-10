// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static void Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var errors = new List<Error>();
            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var (redirectError, redirectUrl) = context.RedirectionProvider.GetRedirectUrl(file.FilePath);
            errors.AddIfNotNull(redirectError);

            var (documentId, documentVersionIndependentId) = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file.FilePath));
            var publishMetadata = new JObject
            {
                ["document_id"] = documentId,
                ["document_version_independent_id"] = documentVersionIndependentId,
                ["canonical_url"] = file.CanonicalUrl,
            };

            context.ErrorLog.Write(errors);
            var outputPath = context.Config.Legacy ? context.DocumentProvider.GetOutputPath(file.FilePath) : null;
            if (context.Config.Legacy && context.DocumentProvider.GetOutputPath(file.FilePath) != null && !context.ErrorLog.HasError(file.FilePath) && !context.Config.DryRun && outputPath != null)
            {
                var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                var metadata = new
                {
                    locale = context.BuildOptions.Locale,
                    monikers,
                    redirect_url = redirectUrl,
                    is_dynamic_rendering = true,
                };

                // Note: produce an empty output to make publish happy
                context.Output.WriteText(outputPath, "{}");
                context.Output.WriteJson(metadataPath, metadata);
            }

            context.PublishModelBuilder.SetPublishItem(file.FilePath, publishMetadata, redirectUrl, outputPath);
        }
    }
}
