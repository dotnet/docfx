// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static (ResourceModel model, List<string> monikers) Build(
            Document file,
            Dictionary<Document, List<string>> monikersMap,
            List<Document> referencingFiles)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);
            Debug.Assert(monikersMap != null);

            var monikers = new List<string>();
            foreach (var referencingFile in referencingFiles)
            {
                if (monikersMap.TryGetValue(referencingFile, out var fileMonikers))
                {
                    monikers.AddRange(fileMonikers);
                }
            }
            monikers.Sort(file.Docset.Monikers.Comparer);
            monikers = monikers.Distinct().ToList();

            return (new ResourceModel
            {
                Locale = file.Docset.Locale,
                Monikers = monikers,
            }, monikers);
        }
    }
}
