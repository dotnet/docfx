// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    public class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly ConcurrentDictionary<string, XRefSpec> _map = new ConcurrentDictionary<string, XRefSpec>();

        internal XrefMap(Docset docset)
        {
            Parallel.ForEach(docset.Config.Xref, url =>
            {
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xrefs) = JsonUtility.Deserialize<List<XRefSpec>>(json);
                foreach (var xref in xrefs)
                {
                    _map.TryAdd(xref.Uid, xref);
                }
            });
        }

        internal XRefSpec Resolve(string uid)
        {
            if (_map.TryGetValue(uid, out var xrefSpec))
            {
                return xrefSpec;
            }
            return null;
        }
    }
}
