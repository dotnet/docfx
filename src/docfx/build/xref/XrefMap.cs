// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> _externalXrefMap;
        private readonly IReadOnlyDictionary<string, InternalXrefSpec> _internalXrefMap;

        private static ThreadLocal<Stack<(string uid, string propertyName, Document parent)>> t_recursionDetector = new ThreadLocal<Stack<(string, string, Document)>>(() => new Stack<(string, string, Document)>());

        public XrefMap(Context context, Docset docset)
        {
            _internalXrefMap = InternalXrefMapBuilder.Build(context);
            _externalXrefMap = ExternalXrefMapLoader.Load(docset);
        }

        public (Error error, string href, string display, IXrefSpec xrefSpec) Resolve(string uid, SourceInfo<string> href, string displayPropertyName, Document relativeTo)
        {
            if (t_recursionDetector.Value.Contains((uid, displayPropertyName, relativeTo)))
            {
                var referenceMap = t_recursionDetector.Value.Select(x => x.parent).ToList();
                referenceMap.Reverse();
                referenceMap.Add(relativeTo);
                throw Errors.CircularReference(referenceMap).ToException();
            }

            try
            {
                t_recursionDetector.Value.Push((uid, displayPropertyName, relativeTo));
                return ResolveCore(uid, href, displayPropertyName);
            }
            finally
            {
                Debug.Assert(t_recursionDetector.Value.Count > 0);
                t_recursionDetector.Value.Pop();
            }
        }

        public XrefMapModel ToXrefMapModel(Context context)
        {
            var references = _internalXrefMap.Values
                .Select(xref => xref.ToExternalXrefSpec(context, forXrefMapOutput: true))
                .OrderBy(xref => xref.Uid).ToArray();

            return new XrefMapModel { References = references };
        }

        private (Error error, string href, string display, IXrefSpec xrefSpec) ResolveCore(
            string uid, SourceInfo<string> href, string displayPropertyName)
        {
            var spec = ResolveXrefSpec(uid);
            if (spec is null)
            {
                return (Errors.XrefNotFound(href), null, null, null);
            }

            var (_, query, fragment) = UrlUtility.SplitUrl(spec.Href);
            var resolvedHref = UrlUtility.MergeUrl(spec.Href, query, fragment.Length == 0 ? "" : fragment.Substring(1));

            var name = spec.GetXrefPropertyValue("name");
            var displayPropertyValue = spec.GetXrefPropertyValue(displayPropertyName);

            // fallback order:
            // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            var display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
            return (null, resolvedHref, display, spec);
        }

        private IXrefSpec ResolveXrefSpec(string uid)
        {
            return ResolveInternalXrefSpec(uid) ?? ResolveExternalXrefSpec(uid);
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
