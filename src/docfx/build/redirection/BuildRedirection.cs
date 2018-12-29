// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static (List<Error> errors, RedirectionModel model, List<string> monikers) Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);
            var (errors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, context.MetadataProvider);

            return (errors, new RedirectionModel
            {
                RedirectUrl = file.RedirectionUrl,
                Locale = file.Docset.Locale,
                Monikers = monikers,
            }, monikers);
        }
    }
}
