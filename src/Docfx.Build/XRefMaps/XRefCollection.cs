// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;

namespace Docfx.Build.Engine;

internal sealed class XRefCollection
{
    private const int MaxParallelism = 0x10;

    public XRefCollection(IEnumerable<Uri> uris)
    {
        ArgumentNullException.ThrowIfNull(uris);

        Uris = uris.ToImmutableList();
    }

    public ImmutableList<Uri> Uris { get; set; }

    public Task<IXRefContainerReader> GetReaderAsync(string baseFolder, IReadOnlyList<string> fallbackFolders = null, CancellationToken cancellationToken = default)
    {
        var creator = new ReaderCreator(Uris, MaxParallelism, baseFolder, fallbackFolders);
        return creator.CreateAsync(cancellationToken);
    }

    private sealed class ReaderCreator
    {
        private readonly ImmutableList<Uri> _uris;
        private readonly HashSet<string> _set = [];
        private readonly Dictionary<Task<IXRefContainer>, Uri> _processing = [];
        private readonly XRefMapDownloader _downloader;

        public ReaderCreator(ImmutableList<Uri> uris, int maxParallelism, string baseFolder, IReadOnlyList<string> fallbackFolders)
        {
            _uris = uris;
            _downloader = new XRefMapDownloader(baseFolder, fallbackFolders, maxParallelism);
        }

        public async Task<IXRefContainerReader> CreateAsync(CancellationToken cancellationToken)
        {
            AddToDownloadList(_uris, cancellationToken);
            var dict = new Dictionary<string, IXRefContainer>();

            while (_processing.Any())
            {
                Task<IXRefContainer> task = await Task.WhenAny(_processing.Keys)
                                                      .WaitAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                Uri uri = _processing[task];
                _processing.Remove(task);
                try
                {
                    IXRefContainer container = await task;
                    if (!container.IsEmbeddedRedirections)
                    {
                        AddToDownloadList(
                            from r in container.GetRedirections()
                            where r != null
                            select GetUri(uri, r.Href) into u
                            where u != null
                            select u,
                            cancellationToken);
                    }
                    dict[uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString] = container;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Unable to download xref map file from {uri}, details: {ex.Message}");
                }
            }
            var fakeEntry = Guid.NewGuid().ToString();
            dict[fakeEntry] = new XRefMap
            {
                HrefUpdated = true,
                Redirections = (from pair in dict
                                select new XRefMapRedirection
                                {
                                    Href = pair.Key,
                                }).ToList()
            };
            return new XRefMapReader(fakeEntry, dict);
        }

        private static Uri GetUri(Uri baseUri, string href)
        {
            if (string.IsNullOrWhiteSpace(href))
            {
                return null;
            }
            if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                return null;
            }
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }
            return new Uri(baseUri, uri);
        }

        private void AddToDownloadList(IEnumerable<Uri> uris, CancellationToken cancellationToken)
        {
            foreach (var uri in uris)
            {
                if (uri.IsAbsoluteUri)
                {
                    if (_set.Add(uri.AbsoluteUri))
                    {
                        var task = _downloader.DownloadAsync(uri, cancellationToken);
                        _processing[task] = uri;
                    }
                }
                else if (_set.Add(uri.OriginalString))
                {
                    var task = _downloader.DownloadAsync(uri, cancellationToken);
                    _processing[task] = uri;
                }
            }
        }
    }
}
