// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Microsoft.Docs.Build;

internal class DocumentProvider
{
    private readonly Input _input;
    private readonly ErrorBuilder _errors;
    private readonly Config _config;
    private readonly BuildScope _buildScope;
    private readonly BuildOptions _buildOptions;
    private readonly JsonSchemaProvider _jsonSchemaProvider;
    private readonly MonikerProvider _monikerProvider;
    private readonly MetadataProvider _metadataProvider;

    private readonly string _depotName;
    private readonly (PathString, DocumentIdConfig)[] _documentIdRules;
    private readonly ReadOnlyDictionary<(string depotName, string sourcePath), (string, string)> _documentIdOverrides;
    private readonly (PathString src, PathString dest)[] _routes;

    private readonly ConcurrentDictionary<FilePath, Watch<Document>> _documents = new();

    // mime -> page type. TODO get from docs-ui schema
    private static readonly Dictionary<string, string> s_pageTypeMapping = new()
    {
        { "NetType", "dotnet" },
        { "NetNamespace", "dotnet" },
        { "NetMember", "dotnet" },
        { "NetEnum", "dotnet" },
        { "NetDelegate", "dotnet" },
        { "RESTOperation", "rest" },
        { "RESTOperationGroup", "rest" },
        { "RESTService", "rest" },
        { "PowershellCmdlet", "powershell" },
        { "PowershellModule", "powershell" },
    };

    public DocumentProvider(
        Input input,
        ErrorBuilder errors,
        Config config,
        BuildOptions buildOptions,
        BuildScope buildScope,
        FileResolver fileResolver,
        JsonSchemaProvider jsonSchemaProvider,
        MonikerProvider monikerProvider,
        MetadataProvider metadataProvider)
    {
        _input = input;
        _errors = errors;
        _config = config;
        _buildOptions = buildOptions;
        _buildScope = buildScope;
        _jsonSchemaProvider = jsonSchemaProvider;
        _monikerProvider = monikerProvider;
        _metadataProvider = metadataProvider;

        var documentIdConfig = config.GlobalMetadata.DocumentIdDepotMapping ?? config.DocumentId;
        _depotName = string.IsNullOrEmpty(config.Product) ? config.Name : $"{config.Product}.{config.Name}";
        _documentIdRules = documentIdConfig.Select(item => (item.Key, item.Value)).OrderByDescending(item => item.Key).ToArray();
        _documentIdOverrides = new(LoadDocumentIdOverrides());
        _routes = config.Routes.Reverse().Select(item => (item.Key, item.Value)).ToArray();

        Dictionary<(string, string), (string, string)> LoadDocumentIdOverrides()
        {
            var result = new Dictionary<(string, string), (string, string)>();
            var documentIdOverride = new DocumentIdOverride();
            if (!string.IsNullOrEmpty(_config.DocumentIdOverride))
            {
                var content = fileResolver.ReadString(_config.DocumentIdOverride);
                documentIdOverride = JsonUtility.DeserializeData<DocumentIdOverride>(content, new FilePath(_config.DocumentIdOverride));
            }

            foreach (var item in documentIdOverride.DocumentIds)
            {
                result.TryAdd((item.DepotName, item.SourcePath.ToLowerInvariant()), (item.DocumentId, item.DocumentVersionIndependentId));
            }

            return result;
        }
    }

    public SourceInfo<string?> GetMime(FilePath path) => GetDocument(path).Mime;

    public ContentType GetContentType(FilePath path) => GetDocument(path).ContentType;

    public string GetOutputPath(FilePath path) => GetDocument(path).OutputPath;

    public string GetSiteUrl(FilePath path) => GetDocument(path).SiteUrl;

    public string GetSitePath(FilePath path) => GetDocument(path).SitePath;

    public string GetCanonicalUrl(FilePath path) => GetDocument(path).CanonicalUrl;

    public RenderType GetRenderType(FilePath path) => GetDocument(path).RenderType;

    [Obsolete("To workaround a docs pdf build image fallback issue. Use GetSiteUrl instead.")]
    public string GetDocsSiteUrl(FilePath path)
    {
        var file = GetDocument(path);
        if (_config.UrlType == UrlType.Docs)
        {
            return file.SiteUrl;
        }

        var sitePath = FilePathToSitePath(path, file.ContentType, UrlType.Docs, file.RenderType);
        return PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), file.ContentType, UrlType.Docs, file.RenderType);
    }

    public string? GetPageType(FilePath file)
    {
        var document = GetDocument(file);
        var mime = document.Mime.Value;

        return document.ContentType switch
        {
            ContentType.Page when mime is null => null,
            ContentType.Page when file.Format == FileFormat.Markdown
                => (_metadataProvider.GetMetadata(_errors, file).Layout ?? mime).ToLowerInvariant(),
            ContentType.Page
                => s_pageTypeMapping.TryGetValue(mime, out var type) ? type : mime.ToLowerInvariant(),
            ContentType.Redirection => "redirection",
            ContentType.Toc => "toc",
            _ => null,
        };
    }

    public (string documentId, string versionIndependentId) GetDocumentId(FilePath path)
    {
        var file = GetDocument(path);

        var depotName = _depotName;
        var sourcePath = path.Path.Value;

        if (TryGetDocumentIdConfig(path.Path, out var config, out var remainingPath))
        {
            if (!string.IsNullOrEmpty(config.DepotName))
            {
                depotName = config.DepotName;
            }

            if (config.FolderRelativePathInDocset != null)
            {
                sourcePath = remainingPath.IsDefault
                    ? config.FolderRelativePathInDocset.Value.Concat(path.Path.GetFileName())
                    : config.FolderRelativePathInDocset.Value.Concat(remainingPath);
            }
        }

        // Remove file extension so .md and .yml files produces the same document id
        sourcePath = Path.ChangeExtension(sourcePath.ToLowerInvariant(), extension: null);

        if (_documentIdOverrides.TryGetValue((depotName, sourcePath), out var result))
        {
            return result;
        }

        // remove file extension from site path
        // site path doesn't contain version info according to the output spec
        var sitePath = Path.ChangeExtension(file.SitePath.ToLowerInvariant(), extension: null);

        var documentId = new Guid(HashUtility.GetSha256HashShort($"{depotName}|{sourcePath}")).ToString();
        var documentVersionIndependentId = new Guid(HashUtility.GetSha256HashShort($"{depotName}|{sitePath}")).ToString();

        return (documentId, documentVersionIndependentId);
    }

    private Document GetDocument(FilePath path)
    {
        return _documents.GetOrAdd(path, key => new(() => GetDocumentCore(key))).Value;
    }

    private bool TryGetDocumentIdConfig(PathString path, out DocumentIdConfig result, out PathString remainingPath)
    {
        foreach (var (basePath, config) in _documentIdRules)
        {
            if (path.StartsWithPath(basePath, out remainingPath))
            {
                result = config;
                return true;
            }
        }
        result = default;
        remainingPath = default;
        return false;
    }

    private Document GetDocumentCore(FilePath path)
    {
        var contentType = _buildScope.GetContentType(path);
        var mime = _input.GetMime(contentType, path);
        var renderType = _jsonSchemaProvider.GetRenderType(contentType, mime);
        var sitePath = FilePathToSitePath(path, contentType, _config.UrlType, renderType);
        var siteUrl = PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), contentType, _config.UrlType, renderType);
        var canonicalUrl = GetCanonicalUrl(sitePath);
        var outputPath = GetOutputPath(path, sitePath, contentType, renderType);

        return new Document(sitePath, siteUrl, outputPath, canonicalUrl, contentType, mime, renderType);
    }

    private string FilePathToSitePath(FilePath filePath, ContentType contentType, UrlType urlType, RenderType renderType)
    {
        var sitePath = ApplyRoutes(filePath.Path).Value;
        if (contentType == ContentType.Page || contentType == ContentType.Redirection || contentType == ContentType.Toc)
        {
            sitePath = renderType == RenderType.Component
                ? Path.ChangeExtension(sitePath, ".json")
                : urlType switch
                {
                    UrlType.Docs => Path.ChangeExtension(sitePath, ".json"),
                    UrlType.Pretty => Path.GetFileNameWithoutExtension(sitePath).Equals("index", PathUtility.PathComparison)
                        ? Path.Combine(Path.GetDirectoryName(sitePath) ?? "", "index.html")
                        : Path.Combine(Path.GetDirectoryName(sitePath) ?? "", Path.GetFileNameWithoutExtension(sitePath).TrimEnd(' ', '.'), "index.html"),
                    UrlType.Ugly => Path.ChangeExtension(sitePath, ".html"),
                    _ => throw new NotSupportedException(),
                };
        }

        if (urlType != UrlType.Docs)
        {
            var monikers = _monikerProvider.GetFileLevelMonikers(_errors, filePath);
            sitePath = Path.Combine(monikers.MonikerGroup ?? "", sitePath);
        }
        if (_config.LowerCaseUrl)
        {
            sitePath = sitePath.ToLowerInvariant();
        }
        return sitePath.Replace('\\', '/');
    }

    private static string PathToAbsoluteUrl(string path, ContentType contentType, UrlType urlType, RenderType renderType)
    {
        var url = PathToRelativeUrl(path, contentType, urlType, renderType);
        return url == "./" ? "/" : "/" + url;
    }

    private static string PathToRelativeUrl(string path, ContentType contentType, UrlType urlType, RenderType renderType)
    {
        var url = path.Replace('\\', '/');

        if (contentType == ContentType.Redirection
            || contentType == ContentType.Toc
            || (contentType == ContentType.Page && renderType == RenderType.Content))
        {
            if (urlType != UrlType.Ugly)
            {
                if (Path.GetFileNameWithoutExtension(path).Equals("index", PathUtility.PathComparison))
                {
                    var i = url.LastIndexOf('/');
                    return i >= 0 ? url[..(i + 1)] : "./";
                }
            }
            if (urlType == UrlType.Docs && contentType != ContentType.Toc)
            {
                var i = url.LastIndexOf('.');
                return i >= 0 ? url[..i] : url;
            }
        }
        return url;
    }

    /// <summary>
    /// The logic is copied from template JINT code
    /// </summary>
    private string GetCanonicalUrl(string? sitePath)
    {
        if (sitePath == null)
        {
            return "";
        }

        var canonicalUrlPrefix = UrlUtility.Combine($"https://{_config.HostName}", _buildOptions.Locale, _config.BasePath);

        var canonicalUrl = canonicalUrlPrefix + "/" + RemoveExtension(EncodePath(sitePath));
        canonicalUrl = canonicalUrl.ToLowerInvariant();

        if (canonicalUrl.EndsWith("/index"))
        {
            canonicalUrl = canonicalUrl[..^5];
        }

        return canonicalUrl;
    }

    private static string EncodePath(string path)
    {
        var splitPaths = path.Split(new char[] { '\\', '/' });
        for (var i = 0; i < splitPaths.Length; i++)
        {
            // ensure all the allowed chars in RFC3986 path-absolute are not encoded
            // including: ALPHA / DIGIT / "-" / "." / "_" / "~" / "%" HEXDIG HEXDIG
            // "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "=" / ":" / "@"
            // reference link: https://dev.azure.com/ceapex/Engineering/_workitems/edit/126389
#pragma warning disable SYSLIB0013 // Type or member is obsolete
            // The logic is copied from template JINT, Uri.EscapeUriString is the only method working same as JS encodeURI function
            splitPaths[i] = Uri.EscapeUriString(splitPaths[i]).Replace("#", "%23").Replace("%25", "%");
#pragma warning restore SYSLIB0013 // Type or member is obsolete
        }
        return string.Join('/', splitPaths);
    }

    private static string RemoveExtension(string path)
    {
        var index = path.LastIndexOf('.');
        if (index > 0)
        {
            return path[..index];
        }
        return path;
    }

    private PathString ApplyRoutes(PathString path)
    {
        (path, _) = _buildScope.MapPath(path);

        // the latter rule takes precedence of the former rule
        foreach (var (source, dest) in _routes)
        {
            if (path.StartsWithPath(source, out var remainingPath))
            {
                if (remainingPath.IsDefault)
                {
                    return dest.Concat(path.GetFileName());
                }
                return dest.Concat(remainingPath);
            }
        }
        return path;
    }

    private string GetOutputPath(FilePath path, string sitePath, ContentType contentType, RenderType renderType)
    {
        var outputPath = sitePath;

        switch (contentType)
        {
            case ContentType.Page:
            case ContentType.Redirection:
                var fileExtension = _config.OutputType switch
                {
                    OutputType.Html => renderType == RenderType.Content ? ".html" : ".json",
                    OutputType.Json => ".json",
                    OutputType.PageJson => renderType == RenderType.Content ? ".raw.page.json" : ".json",
                    _ => throw new NotSupportedException(),
                };
                outputPath = Path.ChangeExtension(outputPath, fileExtension);
                break;

            case ContentType.Toc:
                var tocExtension = _config.OutputType switch
                {
                    OutputType.Html => renderType == RenderType.Content ? ".html" : ".json",
                    OutputType.Json => ".json",
                    OutputType.PageJson => ".json",
                    _ => throw new NotSupportedException(),
                };
                outputPath = Path.ChangeExtension(outputPath, tocExtension);
                break;
        }

        if (_config.UrlType == UrlType.Docs)
        {
            var monikers = _monikerProvider.GetFileLevelMonikers(_errors, path);
            outputPath = UrlUtility.Combine(monikers.MonikerGroup ?? "", outputPath);
        }

        return UrlUtility.Combine(_config.BasePath, outputPath);
    }
}
