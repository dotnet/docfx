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
        private readonly Config _config;
        private readonly Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>> _externalXrefMap;
        private readonly Lazy<IReadOnlyDictionary<string, InternalXrefSpec>> _internalXrefMap;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;
        private readonly Repository? _repository;
        private readonly string _xrefHostName;

        public XrefResolver(
            Context context,
            Config config,
            FileResolver fileResolver,
            Repository? repository,
            DependencyMapBuilder dependencyMapBuilder,
            FileLinkMapBuilder fileLinkMapBuilder)
        {
            _config = config;
            _repository = repository;
            _internalXrefMap = new Lazy<IReadOnlyDictionary<string, InternalXrefSpec>>(
                () => InternalXrefMapBuilder.Build(context));

            _externalXrefMap = new Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>>(
                () => ExternalXrefMapLoader.Load(config, fileResolver));

            _dependencyMapBuilder = dependencyMapBuilder;
            _fileLinkMapBuilder = fileLinkMapBuilder;
            _xrefHostName = string.IsNullOrEmpty(config.XrefHostName) ? config.HostName : config.XrefHostName;
        }

        public (Error? error, string? href, string display, Document? declaringFile) ResolveXref(
            SourceInfo<string> href, Document hrefRelativeTo, Document inclusionRoot)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);
            string? moniker = null;
            string? text = null;
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
            var (xrefError, xrefSpec) = Resolve(new SourceInfo<string>(uid, href.Source), hrefRelativeTo);
            if (xrefError != null || xrefSpec is null)
            {
                return (xrefError, null, "", null);
            }

            var name = xrefSpec.GetXrefPropertyValueAsString("name");
            var displayPropertyValue = displayProperty is null ? null : xrefSpec.GetXrefPropertyValueAsString(displayProperty);

            // fallback order:
            // text -> xrefSpec.displayProperty -> xrefSpec.name -> uid
            var display = !string.IsNullOrEmpty(text) ? text : displayPropertyValue ?? name ?? uid;

            if (!string.IsNullOrEmpty(moniker))
            {
                queries["view"] = moniker;
            }

            var resolvedHref = UrlUtility.MergeUrl(
                RemoveSharingHost(xrefSpec.Href, _config.HostName),
                queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries),
                fragment.Length == 0 ? "" : fragment);

            // NOTE: this should also be relative to root file
            var sourceFile = inclusionRoot ?? hrefRelativeTo;
            _fileLinkMapBuilder.AddFileLink(sourceFile.FilePath, sourceFile.SiteUrl, resolvedHref);

            if (xrefSpec?.DeclaringFile != null && inclusionRoot != null)
            {
                resolvedHref = UrlUtility.GetRelativeUrl(inclusionRoot.SiteUrl, resolvedHref);
            }

            return (null, resolvedHref, display, xrefSpec?.DeclaringFile);
        }

        public (Error?, ExternalXrefSpec?) ResolveXrefSpec(SourceInfo<string> uid, Document referencingFile)
        {
            var (error, xrefSpec) = Resolve(uid, referencingFile);
            return (error, xrefSpec?.ToExternalXrefSpec());
        }

        public XrefMapModel ToXrefMapModel()
        {
            string? repositoryBranch = null;
            string? basePath = null;
            var references = _internalXrefMap.Value.Values
                .Select(xref =>
                {
                    var xrefSpec = xref.ToExternalXrefSpec();
                    if (repositoryBranch is null)
                    {
                        repositoryBranch = _repository?.Branch;
                    }
                    if (basePath is null)
                    {
                        basePath = _config.BasePath.ValueWithLeadingSlash;
                    }

                    // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    var (_, _, fragment) = UrlUtility.SplitUrl(xref.Href);
                    var path = $"https://{_xrefHostName}{xref.DeclaringFile.SiteUrl}";
                    var query = repositoryBranch == "master" ? "?branch=master" : "";
                    xrefSpec.Href = UrlUtility.MergeUrl(path, query, fragment);
                    return xrefSpec;
                })
                .OrderBy(xref => xref.Uid).ToArray();

            var model = new XrefMapModel { References = references };
            if (basePath != null)
            {
                var properties = new XrefProperties();
                properties.Tags.Add(basePath);
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
            if (url.StartsWith($"https://{hostName}/", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring($"https://{hostName}".Length);
            }

            // TODO: this workaround can be removed when all xref related repos migrated to v3
            if (hostName.Equals("docs.microsoft.com", StringComparison.OrdinalIgnoreCase)
                        && url.StartsWith($"https://review.docs.microsoft.com/", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring("https://review.docs.microsoft.com".Length);
            }

            return url;
        }

        private (Error?, IXrefSpec?) Resolve(SourceInfo<string> uid, Document referencingFile)
        {
            var unescapedUid = Uri.UnescapeDataString(uid);
            var xrefSpec = ResolveInternalXrefSpec(unescapedUid, referencingFile) ?? ResolveExternalXrefSpec(unescapedUid);
            if (xrefSpec is null)
            {
                return (Errors.Xref.XrefNotFound(uid), null);
            }
            return (null, xrefSpec);
        }

        private IXrefSpec? ResolveExternalXrefSpec(string uid)
        {
            return _externalXrefMap.Value.TryGetValue(uid, out var result) ? result.Value : null;
        }

        private IXrefSpec? ResolveInternalXrefSpec(string uid, Document declaringFile)
        {
            if (_internalXrefMap.Value.TryGetValue(uid, out var spec))
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, spec.DeclaringFile, DependencyType.UidInclusion);
                return spec;
            }
            return null;
        }
    }
}
