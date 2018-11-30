// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildResource
    {
        internal static ResourceModel Build(
            Document file,
            Dictionary<Document, List<string>> monikersMap,
            List<Document> referencingFiles)
        {
            Debug.Assert(file.ContentType == ContentType.Resource);
            Debug.Assert(referencingFiles.Count > 0);
            Debug.Assert(monikersMap != null);

            var monikers = new List<string>();
            foreach (var referencingFile in referencingFiles)
            {
                if (monikersMap.TryGetValue(referencingFile, out var fileMonikers))
                {
                    monikers.AddRange(fileMonikers);
                }
            }
            monikers.Sort(file.Docset.MonikerAscendingComparer);

            return new ResourceModel
            {
                Locale = file.Docset.Locale,
                Monikers = monikers.Distinct().ToList(),
            };
        }
    }
}
