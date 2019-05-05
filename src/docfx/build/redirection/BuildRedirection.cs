// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static (List<Error> errors, PublishItem publishItem) Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var (errors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, context.MetadataProvider);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                Locale = file.Docset.Locale,
                RedirectUrl = file.RedirectionUrl,
                Monikers = monikers,
            };

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && file.Docset.Legacy)
            {
                var outputPath = file.GetOutputPath(monikers, file.Docset.Config.DocumentId.SiteBasePath, rawPage: true);
                var metadataPath = outputPath.Substring(0, outputPath.Length - ".raw.page.json".Length) + ".mta.json";
                var metadata = new
                {
                    locale = file.Docset.Locale,
                    monikers,
                    redirect_url = file.RedirectionUrl,
                    is_dynamic_rendering = true,
                };

                // Note: produce an empty output to make publish happy
                context.Output.WriteText("{}", outputPath);
                context.Output.WriteJson(metadata, metadataPath);
            }

            return (errors, publishItem);
        }
    }
}
