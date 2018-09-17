// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    public class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, XrefSpec> _map;

        public XrefMap(IReadOnlyDictionary<string, XrefSpec> map)
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

        internal static XrefMap Create(Docset docset)
        {
            HashSet<XrefSpec> map = new HashSet<XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xrefs) = JsonUtility.Deserialize<HashSet<XrefSpec>>(json);
                foreach (var xref in xrefs)
                {
                    map.Add(xref);
                }
            }
            return new XrefMap(map.ToDictionary(xref => xref.Uid));
        }
    }
}
