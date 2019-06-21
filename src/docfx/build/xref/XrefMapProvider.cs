// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class XrefMapProvider
    {
        private Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>> _externalXrefMap;

        // uid --> internal xref specs sorted by moniker
        private Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>> _internalXrefMap;

        private static ThreadLocal<Stack<string>> t_recursionDetector = new ThreadLocal<Stack<string>>(() => new Stack<string>());

        public void Initialize(Context context, Docset docset)
        {
            _externalXrefMap = new Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>>(
                () => ExternalXrefMapLoader.Load(docset));

            _internalXrefMap = new Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>>(
                () => InternalXrefMapBuilder.Build(context, docset));
        }

        public (Error error, IXrefSpec xrefSpec) ResolveXrefSpec(SourceInfo<string> uid, string moniker = null)
        {
            var result = ResolveInternalXrefSpec(uid, moniker);
            if (result != null)
            {
                return (null, result);
            }

            if (_externalXrefMap.Value.TryGetValue(uid, out var externalXrefSpec))
            {
                return (null, externalXrefSpec.Value);
            }

            return (Errors.XrefNotFound(uid), null);
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

        private IXrefSpec ResolveInternalXrefSpec(SourceInfo<string> uid, string moniker)
        {
            var recursionDetector = t_recursionDetector.Value;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.CircularReference(recursionDetector).ToException();
            }

            try
            {
                recursionDetector.Push(uid);

                return ResolveInternalXrefSpecCore(uid, moniker);
            }
            finally
            {
                recursionDetector.Pop();
            }
        }

        private IXrefSpec ResolveInternalXrefSpecCore(SourceInfo<string> uid, string moniker)
        {
            if (!_internalXrefMap.Value.TryGetValue(uid, out var specs))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(moniker))
            {
                foreach (var spec in specs)
                {
                    if (spec.Monikers.Contains(moniker))
                    {
                        return spec;
                    }
                }
            }

            return specs.Length > 0 ? specs[0] : null;
        }
    }
}
