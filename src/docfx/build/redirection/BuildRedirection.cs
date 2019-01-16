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

            if (file.Docset.Legacy)
            {
                publishItem.Path = file.GetOutputPath(monikers);

                var model = new { locale = file.Docset.Locale, monikers, redirect_url = file.RedirectionUrl };
                context.Output.WriteJson(model, publishItem.Path);
            }

            return (errors, publishItem);
        }
    }
}
