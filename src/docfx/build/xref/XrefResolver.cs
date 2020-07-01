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
            Config config,
            FileResolver fileResolver,
            Repository? repository,
            DependencyMapBuilder dependencyMapBuilder,
            FileLinkMapBuilder fileLinkMapBuilder,
            ErrorLog errorLog,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input,
            BuildScope buildScope,
            Lazy<JsonSchemaTransformer> jsonSchemaTransformer)
        {
            _config = config;
            _repository = repository;
            _internalXrefMap = new Lazy<IReadOnlyDictionary<string, InternalXrefSpec>>(
                () => new InternalXrefMapBuilder(
                            errorLog,
                            templateEngine,
                            documentProvider,
                            metadataProvider,
                            monikerProvider,
                            input,
                            buildScope,
                            jsonSchemaTransformer.Value).Build());
            _externalXrefMap = new Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>>(
                () => ExternalXrefMapLoader.Load(config, fileResolver, errorLog));

            _dependencyMapBuilder = dependencyMapBuilder;
            _fileLinkMapBuilder = fileLinkMapBuilder;
            _xrefHostName = string.IsNullOrEmpty(config.XrefHostName) ? config.HostName : config.XrefHostName;
        }

        public (Error? error, string? href, string display, Document? declaringFile) ResolveXrefByHref(
            SourceInfo<string> href, Document referencingFile, Document inclusionRoot)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);

            uid = Uri.UnescapeDataString(uid);

            string? moniker = null;
            string? text = null;
            string? alt = null;
            var queries = new NameValueCollection();
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query);
                moniker = queries["view"];
                queries.Remove("view");
                text = queries["text"];
                queries.Remove("text");
                alt = queries["alt"];
                queries.Remove("alt");
            }
            var displayProperty = queries["displayProperty"];
            queries.Remove("displayProperty");

            // need to url decode uid from input content
            var (xrefError, xrefSpec, resolvedHref) = ResolveXrefSpec(new SourceInfo<string>(uid, href.Source), referencingFile, inclusionRoot);
            if (xrefError != null || xrefSpec is null || resolvedHref == null)
            {
                return (xrefError, null, alt ?? "", null);
            }

            var displayPropertyValue = displayProperty is null ? null : xrefSpec.GetXrefPropertyValueAsString(displayProperty);

            // fallback order:
            // text -> xrefSpec.displayProperty -> xrefSpec.name
            var display = !string.IsNullOrEmpty(text) ? text : displayPropertyValue ?? xrefSpec.Name;

            if (!string.IsNullOrEmpty(moniker))
            {
                queries["view"] = moniker;
            }

            query = queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries);
            var fileLink = UrlUtility.MergeUrl(xrefSpec.Href, query, fragment);
            _fileLinkMapBuilder.AddFileLink(inclusionRoot.FilePath, referencingFile.FilePath, inclusionRoot.SiteUrl, fileLink, href.Source);

            resolvedHref = UrlUtility.MergeUrl(resolvedHref, query, fragment);
            return (null, resolvedHref, display, xrefSpec?.DeclaringFile);
        }

        public (Error? error, string? href, string display, Document? declaringFile) ResolveXrefByUid(
            SourceInfo<string> uid, Document referencingFile, Document inclusionRoot)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return default;
            }

            // need to url decode uid from input content
            var (error, xrefSpec, href) = ResolveXrefSpec(uid, referencingFile, inclusionRoot);
            if (error != null || xrefSpec == null || href == null)
            {
                return (error, null, "", null);
            }
            _fileLinkMapBuilder.AddFileLink(inclusionRoot.FilePath, referencingFile.FilePath, inclusionRoot.SiteUrl, xrefSpec.Href, uid.Source);
            return (null, href, xrefSpec.Name ?? "", xrefSpec.DeclaringFile);
        }

        public (Error?, IXrefSpec?, string? href) ResolveXrefSpec(SourceInfo<string> uid, Document referencingFile, Document inclusionRoot)
        {
            var (error, xrefSpec, href) = Resolve(uid, referencingFile, inclusionRoot);
            if (xrefSpec == null)
            {
                return (error, null, null);
            }
            return (error, xrefSpec, href);
        }

        public XrefMapModel ToXrefMapModel(bool isLocalizedBuild)
        {
            var repositoryBranch = _repository?.Branch;
            var basePath = _config.BasePath.ValueWithLeadingSlash;

            var references =
                isLocalizedBuild
                ? Array.Empty<ExternalXrefSpec>()
                : _internalXrefMap.Value.Values
                .Select(xref =>
                {
                    // DHS appends branch information from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    var query = repositoryBranch == "master" ? "?branch=master" : "";
                    var href = UrlUtility.MergeUrl($"https://{_xrefHostName}{xref.Href}", query);

                    var xrefSpec = xref.ToExternalXrefSpec(href);
                    return xrefSpec;
                })
                .OrderBy(xref => xref.Uid).ToArray();

            var model = new XrefMapModel { References = references };
            if (basePath != null && references.Length > 0)
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

            return url;
        }

        private (Error?, IXrefSpec?, string? href) Resolve(SourceInfo<string> uid, Document referencingFile, Document inclusionRoot)
        {
            var (xrefSpec, href) = ResolveInternalXrefSpec(uid, referencingFile, inclusionRoot);
            if (xrefSpec is null)
            {
                (xrefSpec, href) = ResolveExternalXrefSpec(uid);
            }

            if (xrefSpec is null)
            {
                return (Errors.Xref.XrefNotFound(uid), null, null);
            }

            return (null, xrefSpec, href);
        }

        private (IXrefSpec? xrefSpec, string? href) ResolveExternalXrefSpec(string uid)
        {
            if (_externalXrefMap.Value.TryGetValue(uid, out var spec))
            {
                var href = RemoveSharingHost(spec.Value.Href, _config.RemoveHostName);
                return (spec.Value, href);
            }
            return default;
        }

        private (IXrefSpec?, string? href) ResolveInternalXrefSpec(string uid, Document referencingFile, Document inclusionRoot)
        {
            if (_internalXrefMap.Value.TryGetValue(uid, out var spec))
            {
                _dependencyMapBuilder.AddDependencyItem(referencingFile.FilePath, spec.DeclaringFile.FilePath, DependencyType.Uid, referencingFile.ContentType);
                var href = UrlUtility.GetRelativeUrl((inclusionRoot ?? referencingFile).SiteUrl, spec.Href);
                return (spec, href);
            }
            return default;
        }
    }
}
