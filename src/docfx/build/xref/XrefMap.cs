// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        private readonly IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> _externalXrefMap;
        private readonly IReadOnlyDictionary<string, InternalXrefSpec> _internalXrefMap;

        public XrefMap(Context context, Docset docset, RestoreFileMap restoreFileMap)
        {
            _internalXrefMap = InternalXrefMapBuilder.Build(context);
            _externalXrefMap = ExternalXrefMapLoader.Load(docset, restoreFileMap);
        }

        public IXrefSpec Resolve(SourceInfo<string> uid)
        {
            return ResolveInternalXrefSpec(uid) ?? ResolveExternalXrefSpec(uid);
        }

        public XrefMapModel ToXrefMapModel(Context context)
        {
            var references = _internalXrefMap.Values
                .Select(xref => xref.ToExternalXrefSpec(context, forXrefMapOutput: true))
                .OrderBy(xref => xref.Uid).ToArray();

            return new XrefMapModel { References = references };
        }

        private IXrefSpec ResolveExternalXrefSpec(string uid)
        {
            return _externalXrefMap.TryGetValue(uid, out var result) ? result.Value : null;
        }

        private IXrefSpec ResolveInternalXrefSpec(string uid)
        {
            return _internalXrefMap.TryGetValue(uid, out var spec) ? spec : null;
        }
    }
}
