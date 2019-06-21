// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class XrefMapProvider
    {
        private Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>> _externalXrefMap;
        private Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>> _internalXrefMap;

        public void Initialize(Context context, Docset docset)
        {
            _externalXrefMap = new Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>>(
                () => ExternalXrefMapLoader.Load(docset));

            _internalXrefMap = new Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>>(
                () => InternalXrefMapBuilder.Build(context, docset));
        }

        public (Error error, IXrefSpec[] xrefSpecs) GetXrefSpec(SourceInfo<string> uid)
        {
            if (_internalXrefMap.Value.TryGetValue(uid, out var internalXrefSpecs))
            {
                return (null, internalXrefSpecs);
            }

            if (_externalXrefMap.Value.TryGetValue(uid, out var externalXrefSpec))
            {
                return (null, new IXrefSpec[] { externalXrefSpec.Value });
            }

            return (Errors.XrefNotFound(uid), Array.Empty<IXrefSpec>());
        }

        public XrefMapModel BuildXrefMap()
        {
            var model = new XrefMapModel();

            foreach (var (uid, xrefSpecs) in _internalXrefMap.Value)
            {
                model.References.Add(xrefSpecs.First().ToExternalXrefSpec());
            }

            return model;
        }
    }
}
