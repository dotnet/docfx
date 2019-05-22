// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    public static class MonikerUtility
    {
        private static readonly ConcurrentDictionary<string, string> _groupCache = new ConcurrentDictionary<string, string>();

        public static string GetGroup(List<string> monikers)
        {
            if (monikers == null || monikers.Count == 0)
            {
                return null;
            }
            var monikersKey = string.Join(",", monikers);
            return _groupCache.GetOrAdd(monikersKey, (key) =>
            {
                return HashUtility.GetMd5HashShort(key);
            });
        }
    }
}
