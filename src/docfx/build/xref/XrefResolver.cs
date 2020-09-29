// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Web;

namespace Microsoft.Docs.Build
{
    internal class XrefResolver
    {
        private readonly Config _config;
        private readonly DocumentProvider _documentProvider;
        private readonly ErrorBuilder _errorLog;
        private readonly Lazy<IReadOnlyDictionary<string, Lazy<ExternalXrefSpec>>> _externalXrefMap;
        private readonly Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>> _internalXrefMap;

        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;
        private readonly Repository? _repository;
        private readonly string _xrefHostName;
        private int internalXrefPropertiesValidated;

        public XrefResolver(
            Config config,
            FileResolver fileResolver,
            Repository? repository,
            DependencyMapBuilder dependencyMapBuilder,
            FileLinkMapBuilder fileLinkMapBuilder,
            ErrorBuilder errorLog,
            TemplateEngine templateEngine,
            DocumentProvider documentProvider,
            MetadataProvider metadataProvider,
            MonikerProvider monikerProvider,
            Input input,
            BuildScope buildScope,
            Lazy<JsonSchemaTransformer> jsonSchemaTransformer)
        {
            _config = config;
            _errorLog = errorLog;
            _repository = repository;
            _documentProvider = documentProvider;
            _internalXrefMap = new Lazy<IReadOnlyDictionary<string, InternalXrefSpec[]>>(
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

        public (Error? error, string? href, string display, FilePath? declaringFile) ResolveXrefByHref(
            SourceInfo<string> href, FilePath referencingFile, FilePath inclusionRoot)
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
            if (xrefError != null || xrefSpec is null || string.IsNullOrEmpty(resolvedHref))
            {
                return (xrefError, null, alt ?? "", null);
            }

            var displayPropertyValue = displayProperty is null ? null : xrefSpec.GetXrefPropertyValueAsString(displayProperty);

            // fallback order:
            // text -> xrefSpec.displayProperty -> xrefSpec.name
            var display = !string.IsNullOrEmpty(text) ? text : displayPropertyValue ?? xrefSpec.GetName() ?? xrefSpec.Uid;

            if (!string.IsNullOrEmpty(moniker))
            {
                queries["view"] = moniker;
            }

            query = queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries);
            var fileLink = UrlUtility.MergeUrl(xrefSpec.Href, query, fragment);
            _fileLinkMapBuilder.AddFileLink(inclusionRoot, referencingFile, fileLink, href.Source);

            resolvedHref = UrlUtility.MergeUrl(resolvedHref, query, fragment);
            return (null, resolvedHref, display, xrefSpec?.DeclaringFile);
        }

        public (Error? error, string? href, string display, FilePath? declaringFile) ResolveXrefByUid(
            SourceInfo<string> uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers = null)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return default;
            }

            // need to url decode uid from input content
            var (error, xrefSpec, href) = ResolveXrefSpec(uid, referencingFile, inclusionRoot, monikers);
            if (error != null || xrefSpec == null || href == null)
            {
                return (error, null, "", null);
            }
            _fileLinkMapBuilder.AddFileLink(inclusionRoot, referencingFile, xrefSpec.Href, uid.Source);
            return (null, href, xrefSpec.GetName() ?? xrefSpec.Uid, xrefSpec.DeclaringFile);
        }

        public (Error?, IXrefSpec?, string? href) ResolveXrefSpec(
            SourceInfo<string> uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers = null)
        {
            var (error, xrefSpec, href) = Resolve(uid, referencingFile, inclusionRoot, monikers);
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

            var references = Array.Empty<ExternalXrefSpec>();

            if (!isLocalizedBuild)
            {
                references = EnsureInternalXrefMap().Values
                    .Select(xrefs =>
                    {
                        var xref = xrefs.First();

                        // DHS appends branch information from cookie cache to URL, which is wrong for UID resolved URL
                        // output xref map with URL appending "?branch=master" for master branch
                        var query = _config.UrlType == UrlType.Docs && repositoryBranch != "live"
                            ? $"?branch={repositoryBranch}" : "";

                        var href = UrlUtility.MergeUrl($"https://{_xrefHostName}{xref.Href}", query);

                        return xref.ToExternalXrefSpec(href);
                    })
                    .OrderBy(xref => xref.Uid)
                    .ToArray();
            }

            var model = new XrefMapModel { References = references, RepositoryUrl = _repository?.Remote };

            if (_config.UrlType == UrlType.Docs)
            {
                var properties = new XrefProperties();
                properties.Tags.Add(basePath);
                if (repositoryBranch == "live")
                {
                    properties.Tags.Add("public");
                }
                else
                {
                    properties.Tags.Add("internal");
                }
                model.Properties = properties;
            }

            return model;
        }

        private void ValidateInternalXrefProperties()
        {
            foreach (var xrefs in _internalXrefMap.Value.Values)
            {
                if (xrefs.Length == 1)
                {
                    continue;
                }

                var uid = xrefs.First().Uid;

                // validate xref properties
                // uid conflicts with different values of the same xref property
                // log an warning and take the first one order by the declaring file
                var xrefProperties = xrefs.SelectMany(x => x.XrefProperties.Keys).Distinct();
                foreach (var xrefProperty in xrefProperties)
                {
                    var conflictingNames = xrefs.Select(x => x.GetXrefPropertyValueAsString(xrefProperty)).Distinct();
                    if (conflictingNames.Count() > 1)
                    {
                        _errorLog.Add(Errors.Xref.XrefPropertyConflict(uid, xrefProperty, conflictingNames));
                    }
                }
            }
        }

        private void ValidateUIDGlobalUnique()
        {
            var globalUIDs = _internalXrefMap.Value.Values.Where(xrefs => xrefs.Any(xref => xref.UIDGlobalUnique)).Select(xrefs => xrefs.First().Uid);

            foreach (var uid in globalUIDs)
            {
                if (_externalXrefMap.Value.TryGetValue(uid.Value, out var spec) && spec?.Value != null)
                {
                    _errorLog.Add(Errors.Xref.DuplicateUidGlobal(uid, spec.Value.RepositoryUrl));
                }
            }
        }

        private static string RemoveSharingHost(string url, string hostName)
        {
            // TODO: this workaround can be removed when all xref related repos migrated to v3
            if (hostName.Equals("docs.microsoft.com", StringComparison.OrdinalIgnoreCase)
                        && url.StartsWith($"https://review.docs.microsoft.com/", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring("https://review.docs.microsoft.com".Length);
            }

            if (url.StartsWith($"https://{hostName}/", StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring($"https://{hostName}".Length);
            }

            return url;
        }

        private (Error?, IXrefSpec?, string? href) Resolve(
            SourceInfo<string> uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers = null)
        {
            var (xrefSpec, href) = ResolveInternalXrefSpec(uid, referencingFile, inclusionRoot, monikers);
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
                var href = RemoveSharingHost(spec.Value.Href, _config.HostName);
                return (spec.Value, href);
            }
            return default;
        }

        private IReadOnlyDictionary<string, InternalXrefSpec[]> EnsureInternalXrefMap()
        {
            if (!_internalXrefMap.IsValueCreated)
            {
                _ = _internalXrefMap.Value;
            }

            if (Interlocked.Exchange(ref internalXrefPropertiesValidated, 1) == 0)
            {
                ValidateInternalXrefProperties();
                ValidateUIDGlobalUnique();
            }

            return _internalXrefMap.Value;
        }

        private (IXrefSpec?, string? href) ResolveInternalXrefSpec(
            string uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers = null)
        {
            if (EnsureInternalXrefMap().TryGetValue(uid, out var specs))
            {
                var spec = default(InternalXrefSpec);
                if (specs.Length == 1 || !monikers.HasValue || !monikers.Value.HasMonikers)
                {
                    spec = specs[0];
                }
                else
                {
                    spec = specs.FirstOrDefault(s => s.Monikers.Intersects(monikers.Value)) ?? specs[0];
                }

                var dependencyType = GetDependencyType(referencingFile, spec);
                _dependencyMapBuilder.AddDependencyItem(referencingFile, spec.DeclaringFile, dependencyType);

                var href = UrlUtility.GetRelativeUrl(_documentProvider.GetSiteUrl(inclusionRoot), spec.Href);
                return (spec, href);
            }
            return default;
        }

        private DependencyType GetDependencyType(FilePath referencingFile, InternalXrefSpec xref)
        {
            var mime = _documentProvider.GetMime(referencingFile).Value;

            if (!string.Equals(mime, "LearningPath", StringComparison.Ordinal) &&
                !string.Equals(mime, "Module", StringComparison.Ordinal))
            {
                return DependencyType.Uid;
            }

            var declaringFileMime = _documentProvider.GetMime(xref.DeclaringFile).Value;

            switch ((mime, declaringFileMime))
            {
                case ("LearningPath", "Module"):
                case ("Module", "ModuleUnit"):
                    return DependencyType.Hierarchy;
                case ("LearningPath", "Achievement"):
                case ("Module", "Achievement"):
                case ("LearningPath", "LearningPath") when string.Equals(xref.DeclaringPropertyPath, "trophy", StringComparison.OrdinalIgnoreCase):
                case ("Module", "Module") when string.Equals(xref.DeclaringPropertyPath, "badge", StringComparison.OrdinalIgnoreCase):
                    return DependencyType.Achievement;
            }

            return DependencyType.Uid;
        }
    }
}
