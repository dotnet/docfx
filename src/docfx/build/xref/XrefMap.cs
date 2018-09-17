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
        private readonly ConcurrentDictionary<string, XrefSpec> _map = new ConcurrentDictionary<string, XrefSpec>();

        internal XrefMap(Docset docset)
        {
            Parallel.ForEach(docset.Config.Xref, url =>
            {
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xrefs) = JsonUtility.Deserialize<List<XrefSpec>>(json);
                foreach (var xref in xrefs)
                {
                    _map.TryAdd(xref.Uid, xref);
                }
            });
        }

        internal XrefSpec Resolve(string uid)
        {
            if (_map.TryGetValue(uid, out var xrefSpec))
            {
                return xrefSpec;
            }
            return null;
        }
    }
}
