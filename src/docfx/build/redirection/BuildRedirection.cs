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
                Group = HashUtility.GetMd5HashShort(monikers),
            };

            if (file.Docset.Legacy)
            {
                publishItem.Path = file.GetOutputPath(monikers, file.Docset.SiteBasePath, rawPage: true);
            }

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && file.Docset.Legacy)
            {
                var metadataPath = publishItem.Path.Substring(0, publishItem.Path.Length - ".raw.page.json".Length) + ".mta.json";
                var metadata = new
                {
                    locale = file.Docset.Locale,
                    monikers,
                    redirect_url = file.RedirectionUrl,
                    is_dynamic_rendering = true,
                };

                // Note: produce an empty output to make publish happy
                context.Output.WriteText("{}", publishItem.Path);
                context.Output.WriteJson(metadata, metadataPath);
            }

            return (errors, publishItem);
        }
    }
}
