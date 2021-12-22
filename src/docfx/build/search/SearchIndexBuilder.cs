// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class SearchIndexBuilder
{
    private readonly Config _config;
    private readonly ErrorBuilder _errors;
    private readonly DocumentProvider _documentProvider;
    private readonly MetadataProvider _metadataProvider;

    private readonly Scoped<ConcurrentDictionary<FilePath, SearchIndexItem>> _searchIndex = new();

    public SearchIndexBuilder(Config config, ErrorBuilder errors, DocumentProvider documentProvider, MetadataProvider metadataProvider)
    {
        _config = config;
        _errors = errors;
        _documentProvider = documentProvider;
        _metadataProvider = metadataProvider;
    }

    public void SetTitle(FilePath file, string? title)
    {
        if (string.IsNullOrEmpty(title) || _config.SearchEngine != SearchEngineType.Lunr || NoIndex(file))
        {
            return;
        }

        Watcher.Write(() => _searchIndex.Value.GetOrAdd(file, _ => new SearchIndexItem(_documentProvider.GetSiteUrl(file))).Title = title);
    }

    public void SetBody(FilePath file, string? body)
    {
        if (string.IsNullOrEmpty(body) || _config.SearchEngine != SearchEngineType.Lunr || NoIndex(file))
        {
            return;
        }

        Watcher.Write(() => _searchIndex.Value.GetOrAdd(file, _ => new SearchIndexItem(_documentProvider.GetSiteUrl(file))).Body = body);
    }

    public string? Build()
    {
        if (_searchIndex.Value.IsEmpty)
        {
            return null;
        }

        var documents = JToken.FromObject(_searchIndex.Value.Values);
        using var js = JavaScriptEngine.Create(new LocalPackage());
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "data/scripts/lunr.interop.js");

        return js.Run(scriptPath, "transform", documents).ToString();
    }

    private bool NoIndex(FilePath file)
    {
        return _metadataProvider.GetMetadata(_errors, file).NoIndex();
    }
}
