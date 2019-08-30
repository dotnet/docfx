// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var errors = new List<Error>();
            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(monikerError);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                SourcePath = file.FilePath.Path,
                Locale = file.Docset.Locale,
                RedirectUrl = file.RedirectionUrl,
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
            };

            context.PublishModelBuilder.TryAdd(file, publishItem);

            return errors;
        }
    }
}
