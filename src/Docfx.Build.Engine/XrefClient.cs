// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class XrefClient
{
    public static readonly XrefClient Default = new();
    private static readonly HttpClient _sharedClient =
        new Func<HttpClient>(() =>
        {
            var client = new HttpClient(new HttpClientHandler { CheckCertificateRevocationList = true });
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        })();
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _semaphore;

    public XrefClient()
        : this(_sharedClient, null) { }

    public XrefClient(HttpClient client)
        : this(null, null) { }

    public XrefClient(int maxParallelism)
        : this(null, new SemaphoreSlim(maxParallelism)) { }

    public XrefClient(SemaphoreSlim semaphore)
        : this(null, semaphore) { }

    public XrefClient(HttpClient client, int maxParallelism)
        : this(client, new SemaphoreSlim(maxParallelism)) { }

    public XrefClient(HttpClient client, SemaphoreSlim semaphore)
    {
        _client = client ?? _sharedClient;
        _semaphore = semaphore;
    }

    public async Task<List<XRefSpec>> ResolveAsync(string url)
    {
        if (_semaphore == null)
        {
            return await ResolveCoreAsync(url);
        }
        await _semaphore.WaitAsync();
        try
        {
            return await ResolveCoreAsync(url);
        }
        finally
        {
            if (_semaphore != null)
            {
                _semaphore.Release();
            }
        }
    }

    private async Task<List<XRefSpec>> ResolveCoreAsync(string url)
    {
        using var stream = await _client.GetStreamAsync(url);
        using var sr = new StreamReader(stream);
        var xsList = JsonUtility.Deserialize<List<Dictionary<string, object>>>(sr);
        return xsList.ConvertAll(item =>
        {
            var spec = new XRefSpec();
            foreach (var pair in item)
            {
                if (pair.Value is string s)
                {
                    spec[pair.Key] = s;
                }
            }
            return spec;
        });
    }
}
