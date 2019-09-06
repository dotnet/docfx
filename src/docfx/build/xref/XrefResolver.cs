// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace Microsoft.Docs.Build
{
    internal class XrefResolver
    {
        private readonly IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>> _externalXrefMap;
        private readonly IReadOnlyDictionary<string, InternalXrefSpec> _internalXrefMap;
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        public XrefResolver(Context context, Docset docset, RestoreFileMap restoreFileMap, DependencyMapBuilder dependencyMapBuilder)
        {
            _internalXrefMap = InternalXrefMapBuilder.Build(context);
            _externalXrefMap = ExternalXrefMapLoader.Load(docset, restoreFileMap);
            _dependencyMapBuilder = dependencyMapBuilder;
        }

        public (Error error, string href, string display, Document declaringFile) ResolveAbsoluteXref(SourceInfo<string> href, Document referencingFile)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);
            string moniker = null;
            string text = null;
            var queries = new NameValueCollection();
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query);
                moniker = queries["view"];
                queries.Remove("view");
                text = queries["text"];
                queries.Remove("text");
            }
            var displayProperty = queries["displayProperty"];
            queries.Remove("displayProperty");

            // need to url decode uid from input content
            var (xrefError, xrefSpec) = Resolve(new SourceInfo<string>(uid, href.Source), referencingFile);
            if (xrefError != null)
            {
                return (xrefError, null, null, null);
            }

            var name = xrefSpec.GetXrefPropertyValueAsString("name");
            var displayPropertyValue = xrefSpec.GetXrefPropertyValueAsString(displayProperty);

            // fallback order:
            // text -> xrefSpec.displayProperty -> xrefSpec.name -> uid
            var display = !string.IsNullOrEmpty(text) ? text : displayPropertyValue ?? name ?? uid;

            if (!string.IsNullOrEmpty(moniker))
            {
                queries["view"] = moniker;
            }
            var resolvedHref = UrlUtility.MergeUrl(
                RemoveSharingHost(xrefSpec.Href, referencingFile.Docset.HostName),
                queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries),
                fragment.Length == 0 ? "" : fragment.Substring(1));

            return (null, resolvedHref, display, xrefSpec?.DeclaringFile);
        }

        public (Error error, string href, string display, Document declaringFile) ResolveRelativeXref(
            Document relativeToFile, SourceInfo<string> href, Document referencingFile)
        {
            var (error, link, display, declaringFile) = ResolveAbsoluteXref(href, referencingFile);

            if (declaringFile != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, display, declaringFile);
        }

        public (Error, ExternalXrefSpec) ResolveXrefSpec(SourceInfo<string> uid, Document referencingFile)
        {
            var (error, xrefSpec) = Resolve(uid, referencingFile);
            return (error, xrefSpec?.ToExternalXrefSpec());
        }

        public XrefMapModel ToXrefMapModel()
        {
            string repositoryBranch = null;
            string siteBasePath = null;
            var references = _internalXrefMap.Values
                .Select(xref =>
                {
                    var xrefSpec = xref.ToExternalXrefSpec();
                    if (repositoryBranch is null)
                    {
                        repositoryBranch = xref.DeclaringFile.Docset.Repository?.Branch;
                    }
                    if (siteBasePath is null)
                    {
                        siteBasePath = xref.DeclaringFile.Docset.SiteBasePath;
                    }

                    // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    var (_, _, fragment) = UrlUtility.SplitUrl(xref.Href);
                    var path = xref.DeclaringFile.CanonicalUrlWithoutLocale;
                    var query = repositoryBranch == "master" ? "?branch=master" : "";
                    xrefSpec.Href = path + query + fragment;
                    return xrefSpec;
                })
                .OrderBy(xref => xref.Uid).ToArray();

            var model = new XrefMapModel { References = references };
            if (siteBasePath != null)
            {
                var properties = new XrefProperties();
                properties.Tags.Add($"/{siteBasePath}");
                if (repositoryBranch == "master")
                {
                    properties.Tags.Add("internal");
                }
                else if (repositoryBranch == "live")
                {
                    properties.Tags.Add("public");
                }
                model.Properties = properties;
            }

            return model;
        }

        private string RemoveSharingHost(string url, string hostName)
        {
            if (url.StartsWith($"{hostName}/", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(hostName.Length);
            }

            return url;
        }

        private (Error, IXrefSpec) Resolve(SourceInfo<string> uid, Document referencingFile)
        {
            var unescapedUid = Uri.UnescapeDataString(uid);
            var xrefSpec = ResolveInternalXrefSpec(unescapedUid, referencingFile) ?? ResolveExternalXrefSpec(unescapedUid);
            if (xrefSpec is null)
            {
                return (Errors.XrefNotFound(uid), null);
            }
            return (null, xrefSpec);
        }

        private IXrefSpec ResolveExternalXrefSpec(string uid)
        {
            return _externalXrefMap.TryGetValue(uid, out var result) ? result.Value : null;
        }

        private IXrefSpec ResolveInternalXrefSpec(string uid, Document declaringFile)
        {
            if (_internalXrefMap.TryGetValue(uid, out var spec))
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, spec.DeclaringFile, DependencyType.UidInclusion);
                return spec;
            }
            return null;
        }
    }
}
