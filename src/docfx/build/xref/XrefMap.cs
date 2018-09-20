// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    public class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly InternalXrefMap _internalXrefMap;
        private readonly IReadOnlyDictionary<string, XrefSpec> _externalXrefMap;

        private XrefMap(IReadOnlyDictionary<string, XrefSpec> map, InternalXrefMap internalXrefMap)
        {
            _externalXrefMap = map;
            _internalXrefMap = internalXrefMap;
        }

        public XrefSpec Resolve(string uid)
        {
            if (_internalXrefMap != null && _internalXrefMap.Resolve(uid, out var xrefSpec))
            {
                return xrefSpec;
            }
            if (_externalXrefMap.TryGetValue(uid, out xrefSpec))
            {
                return xrefSpec;
            }
            return null;
        }

        internal static async Task<XrefMap> Create(Context context, Docset docset, bool buildInternalXrefMap)
        {
            Dictionary<string, XrefSpec> map = new Dictionary<string, XrefSpec>();
            foreach (var url in docset.Config.Xref)
            {
                var json = File.ReadAllText(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, url));
                var (_, xRefMap) = JsonUtility.Deserialize<XrefMapModel>(json);
                foreach (var sepc in xRefMap.References)
                {
                    map[sepc.Uid] = sepc;
                }
            }
            return new XrefMap(map, buildInternalXrefMap ? await InternalXrefMap.Create(context, docset.BuildScope) : null);
        }
    }
}
