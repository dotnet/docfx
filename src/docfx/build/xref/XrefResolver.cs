// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Specialized;
using System.Web;

namespace Microsoft.Docs.Build;

internal class XrefResolver
{
    private readonly Config _config;
    private readonly DocumentProvider _documentProvider;
    private readonly RedirectionProvider _redirectionProvider;
    private readonly ErrorBuilder _errorLog;
    private readonly DependencyMapBuilder _dependencyMapBuilder;
    private readonly FileLinkMapBuilder _fileLinkMapBuilder;
    private readonly Repository? _repository;
    private readonly string _xrefHostName;
    private readonly InternalXrefMapBuilder _internalXrefMapBuilder;
    private readonly Func<JsonSchemaTransformer> _jsonSchemaTransformer;

    private readonly Watch<ExternalXrefMap> _externalXrefMap;
    private readonly Watch<IReadOnlyDictionary<string, InternalXrefSpec[]>> _internalXrefMap;

    public XrefResolver(Config config,
        FileResolver fileResolver,
        Repository? repository,
        DependencyMapBuilder dependencyMapBuilder,
        FileLinkMapBuilder fileLinkMapBuilder,
        ErrorBuilder errorLog,
        DocumentProvider documentProvider,
        MetadataProvider metadataProvider,
        MonikerProvider monikerProvider,
        BuildScope buildScope,
        RepositoryProvider repositoryProvider,
        Input input,
        RedirectionProvider redirectionProvider,
        Func<JsonSchemaTransformer> jsonSchemaTransformer)
    {
        _config = config;
        _errorLog = errorLog;
        _repository = repository;
        _documentProvider = documentProvider;
        _jsonSchemaTransformer = jsonSchemaTransformer;
        _redirectionProvider = redirectionProvider;
        _dependencyMapBuilder = dependencyMapBuilder;
        _fileLinkMapBuilder = fileLinkMapBuilder;
        _xrefHostName = string.IsNullOrEmpty(config.XrefHostName) ? config.HostName : config.XrefHostName;
        _internalXrefMapBuilder = new(
            config, errorLog, documentProvider, metadataProvider, monikerProvider, buildScope, repositoryProvider,
            input, _redirectionProvider, jsonSchemaTransformer);

        _externalXrefMap = new(() => ExternalXrefMapLoader.Load(config, fileResolver, errorLog));
        _internalXrefMap = new(BuildInternalXrefMap);
    }

    public (Error? error, XrefLink xrefLink) ResolveXrefByHref(
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
            return (xrefError, new XrefLink(null, alt ?? "", null, !string.IsNullOrEmpty(alt)));
        }

        var displayPropertyValue = displayProperty is null ? null : xrefSpec.GetXrefPropertyValueAsString(displayProperty);

        // fallback order:
        // text -> xrefSpec.displayProperty -> xrefSpec.name
        var display = !string.IsNullOrEmpty(text) ? text : displayPropertyValue ?? xrefSpec.GetName() ?? xrefSpec.Uid;

        var localizable = false;
        if (!string.IsNullOrEmpty(text) || IsNameLocalizable(xrefSpec))
        {
            localizable = true;
        }

        if (!string.IsNullOrEmpty(moniker))
        {
            queries["view"] = moniker;
        }

        query = queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries);
        var fileLink = UrlUtility.MergeUrl(xrefSpec.Href, query, fragment);
        _fileLinkMapBuilder.AddFileLink(inclusionRoot, referencingFile, fileLink, href.Source);

        resolvedHref = UrlUtility.MergeUrl(resolvedHref, query, fragment);
        return (null, new XrefLink(resolvedHref, display, xrefSpec.DeclaringFile, localizable));
    }

    public (Error? error, XrefLink xrefLink) ResolveXrefByUid(
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
            return (error, new XrefLink(null, "", null, false));
        }

        var localizable = IsNameLocalizable(xrefSpec);
        _fileLinkMapBuilder.AddFileLink(inclusionRoot, referencingFile, xrefSpec.Href, uid.Source);
        return (null, new XrefLink(href, xrefSpec.GetName() ?? xrefSpec.Uid, xrefSpec.DeclaringFile, localizable));
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

    public XrefMapModel ToXrefMapModel()
    {
        var repositoryBranch = _repository?.Branch;
        var basePath = _config.BasePath.ValueWithLeadingSlash;

        var references = Array.Empty<ExternalXrefSpec>();
        var externalXrefs = Array.Empty<ExternalXref>();

        references = _internalXrefMap.Value.Values
            .Select(xrefs =>
            {
                var xref = xrefs.First();

                // DHS appends branch information from cookie cache to URL, which is wrong for UID resolved URL
                // output xref map with URL appending "?branch=master" for master branch
                var query = _config.UrlType == UrlType.Docs && repositoryBranch != "live" ? $"?branch={repositoryBranch}" : "";
                var href = UrlUtility.MergeUrl($"https://{_xrefHostName}{xref.Href}", query);
                return xref.ToExternalXrefSpec(href);
            })
            .OrderBy(xref => xref.Uid)
            .ToArray();

        var monikerGroups = new Dictionary<string, MonikerList>(
            from item in references
            let monikerGroup = item.MonikerGroup
            where !string.IsNullOrEmpty(monikerGroup)
            orderby monikerGroup
            group item by monikerGroup into g
            select new KeyValuePair<string, MonikerList>(g.Key, g.First().Monikers));

        externalXrefs = _jsonSchemaTransformer().GetValidateExternalXrefs();

        XrefProperties? properties = null;
        if (_config.UrlType == UrlType.Docs)
        {
            properties = new XrefProperties();
            properties.Tags.Add(basePath);
            if (repositoryBranch == "live")
            {
                properties.Tags.Add("public");
            }
            else
            {
                properties.Tags.Add("internal");
            }
        }

        var model =
            new XrefMapModel
            {
                References = references,
                ExternalXrefs = externalXrefs,
                RepositoryUrl = _repository?.Url,
                DocsetName = _config.Name.Value,
                MonikerGroups = monikerGroups,
                Properties = properties,
            };

        return model;
    }

    private IReadOnlyDictionary<string, InternalXrefSpec[]> BuildInternalXrefMap()
    {
        var result = _internalXrefMapBuilder.Build();
        ValidateUidGlobalUnique(result);
        ValidateExternalXref(result);
        return result;
    }

    private static bool IsNameLocalizable(IXrefSpec xrefSpec)
        => xrefSpec is InternalXrefSpec internalXrefSpec && internalXrefSpec.IsNameLocalizable;

    private void ValidateUidGlobalUnique(IReadOnlyDictionary<string, InternalXrefSpec[]> internalXrefMap)
    {
        var globalXrefSpecs = internalXrefMap.Values.Where(xrefs => xrefs.Any(xref => xref.UidGlobalUnique)).Select(xrefs => xrefs.First());

        foreach (var xrefSpec in globalXrefSpecs)
        {
            if (_externalXrefMap.Value.TryGetValue(xrefSpec.Uid, out var spec))
            {
                _errorLog.Add(
                    Errors.Xref.DuplicateUidGlobal(xrefSpec.Uid, spec!.RepositoryUrl, xrefSpec.PropertyPath) with
                    { Level = _config.IsLearn ? ErrorLevel.Error : ErrorLevel.Warning });
            }
        }
    }

    private void ValidateExternalXref(IReadOnlyDictionary<string, InternalXrefSpec[]> internalXrefMap)
    {
        var localXrefGroups = _externalXrefMap.Value.GetExternalXrefs()
            .Where(xref => string.Equals(xref.DocsetName, _config.Name, StringComparison.OrdinalIgnoreCase))
            .GroupBy(xref => xref.Uid);

        foreach (var xrefGroup in localXrefGroups)
        {
            if (!internalXrefMap.ContainsKey(xrefGroup.Key))
            {
                foreach (var item in xrefGroup)
                {
                    _errorLog.Add(Errors.Xref.UidNotFound(
                        xrefGroup.Key,
                        repository: item.ReferencedRepositoryUrl,
                        item.SchemaType,
                        item.PropertyPath) with { Level = _config.IsLearn ? ErrorLevel.Error : ErrorLevel.Warning });
                }
            }
        }
    }

    private static string RemoveSharingHost(string url, string hostName)
    {
        // TODO: this workaround can be removed when all xref related repos migrated to v3
        if (hostName.Equals("docs.microsoft.com", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith($"https://review.docs.microsoft.com/", StringComparison.OrdinalIgnoreCase))
        {
            return url["https://review.docs.microsoft.com".Length..];
        }

        if (url.StartsWith($"https://{hostName}/", StringComparison.OrdinalIgnoreCase))
        {
            return url[$"https://{hostName}".Length..];
        }

        return url;
    }

    private (Error?, IXrefSpec?, string? href) Resolve(
        SourceInfo<string> uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers)
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
            var href = RemoveSharingHost(spec!.Href, _config.HostName);
            return (spec, href);
        }
        return default;
    }

    private (IXrefSpec?, string? href) ResolveInternalXrefSpec(
        string uid, FilePath referencingFile, FilePath inclusionRoot, MonikerList? monikers)
    {
        if (_internalXrefMap.Value.TryGetValue(uid, out var specs))
        {
            var spec = specs.Length == 1 || !monikers.HasValue || !monikers.Value.HasMonikers
                ? specs[0]
                : specs.FirstOrDefault(s => s.Monikers.Intersects(monikers.Value)) ?? specs[0];

            var dependencyType = GetDependencyType(referencingFile, spec);
            _dependencyMapBuilder.AddDependencyItem(referencingFile, spec.DeclaringFile, dependencyType);

            // Output absolute URL starting from Architecture and TSType
            var href = JsonSchemaProvider.OutputAbsoluteUrl(_documentProvider.GetMime(inclusionRoot))
                ? spec.Href
                : UrlUtility.GetRelativeUrl(_documentProvider.GetSiteUrl(inclusionRoot), spec.Href);

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
            case ("LearningPath", "LearningPath") when string.Equals(xref.SchemaType, "trophy", StringComparison.OrdinalIgnoreCase):
            case ("Module", "Module") when string.Equals(xref.SchemaType, "badge", StringComparison.OrdinalIgnoreCase):
                return DependencyType.Achievement;
        }

        return DependencyType.Uid;
    }
}
