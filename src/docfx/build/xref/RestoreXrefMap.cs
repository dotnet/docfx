// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    public class RestoreXrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, XrefSpec> _map;

        private RestoreXrefMap(IReadOnlyDictionary<string, XrefSpec> map)
        {
            _map = map;
        }

        public XrefSpec Resolve(string uid)
        {
            if (_map.TryGetValue(uid, out var xrefSpec))
            {
                return xrefSpec;
            }
            return null;
        }

        internal static RestoreXrefMap Create(Docset docset)
        {
            Dictionary<string, XrefSpec> map = new Dictionary<string, XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xRefMap) = JsonUtility.Deserialize<XrefMap>(json);
                foreach (var sepc in xRefMap.References)
                {
                    map[sepc.Uid] = sepc;
                }
            }
            return new RestoreXrefMap(map);
        }
    }
}
