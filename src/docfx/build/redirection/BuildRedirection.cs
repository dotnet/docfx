// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var errors = new List<Error>();
            var (monikerErrors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddRange(monikerErrors);

            var publishItem = context.PublishModelBuilder.GetPublishItem(file.FilePath);
            var (redirectError, redirectUrl) = context.RedirectionProvider.GetRedirectUrl(file.FilePath);
            errors.AddIfNotNull(redirectError);
            publishItem.RedirectUrl = redirectUrl;

            var (documentId, documentVersionIndependentId) = context.DocumentProvider.GetDocumentId(context.RedirectionProvider.GetOriginalFile(file.FilePath));
            publishItem.ExtensionData = new JObject
            {
                ["document_id"] = documentId,
                ["document_version_independent_id"] = documentVersionIndependentId,
                ["canonical_url"] = file.CanonicalUrl,
            };

            if (context.Config.Legacy && context.DocumentProvider.GetOutputPath(file.FilePath) != null && !context.Config.DryRun)
            {
                if (errors.Any(x => x.Level == ErrorLevel.Error))
                {
                    publishItem.HasError = true;
                    return errors;
                }
                var metadataPath = publishItem.Path!.Substring(0, publishItem.Path.Length - ".raw.page.json".Length) + ".mta.json";
                var metadata = new
                {
                    locale = context.BuildOptions.Locale,
                    monikers,
                    redirect_url = publishItem.RedirectUrl,
                    is_dynamic_rendering = true,
                };

                // Note: produce an empty output to make publish happy
                context.Output.WriteText(publishItem.Path, "{}");
                context.Output.WriteJson(metadataPath, metadata);
            }

            return errors;
        }
    }
}
