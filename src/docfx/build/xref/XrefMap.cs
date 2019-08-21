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
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        public XrefMap(Context context, Docset docset, RestoreFileMap restoreFileMap, DependencyMapBuilder dependencyMapBuilder)
        {
            _internalXrefMap = InternalXrefMapBuilder.Build(context);
            _externalXrefMap = ExternalXrefMapLoader.Load(docset, restoreFileMap);
            _dependencyMapBuilder = dependencyMapBuilder;
        }

        public (Error, IXrefSpec) Resolve(SourceInfo<string> uid, Document declaringFile)
        {
            var xrefSpec = ResolveInternalXrefSpec(uid, declaringFile) ?? ResolveExternalXrefSpec(uid);
            if (xrefSpec is null)
            {
                return (Errors.XrefNotFound(uid), null);
            }
            return (null, xrefSpec);
        }

        public XrefMapModel ToXrefMapModel()
        {
            var references = _internalXrefMap.Values
                .Select(xref =>
                {
                    var xrefSpec = xref.ToExternalXrefSpec();

                    // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    var (_, _, fragment) = UrlUtility.SplitUrl(xref.Href);
                    var path = xref.DeclaringFile.CanonicalUrlWithoutLocale;
                    var query = xref.DeclaringFile.Docset.Repository?.Branch == "master" ? "?branch=master" : "";
                    xrefSpec.Href = path + query + fragment;
                    return xrefSpec;
                })
                .OrderBy(xref => xref.Uid).ToArray();

            return new XrefMapModel { References = references };
        }

        private IXrefSpec ResolveExternalXrefSpec(string uid)
        {
            return _externalXrefMap.TryGetValue(uid, out var result) ? result.Value : null;
        }

        private IXrefSpec ResolveInternalXrefSpec(string uid, Document declaringFile)
        {
            if (!_internalXrefMap.TryGetValue(uid, out var spec))
            {
                return null;
            }
            else
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, spec.DeclaringFile, DependencyType.UidInclusion);
                return spec;
            }
        }
    }
}
