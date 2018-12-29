// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static (List<Error> errors, ResourceModel model, List<string> monikers) Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);

            var (errors, monikers) = context.MonikerProvider.GetFileLevelMonikers(file, context.MetadataProvider);
            return (errors, new ResourceModel
            {
                Locale = file.Docset.Locale,
                Monikers = monikers,
            }, monikers);
        }
    }
}
