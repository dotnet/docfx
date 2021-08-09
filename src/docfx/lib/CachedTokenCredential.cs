using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Docs.Build
{
    public class CachedTokenCredential : TokenCredential
    {
        private static readonly TimeSpan s_tokenRefreshTime = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, AccessToken> _cache = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly TokenCredential _tokenCredential;

        public CachedTokenCredential(TokenCredential tokenCredential) => _tokenCredential = tokenCredential;

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateCacheKey(requestContext);
            if (TryGetFromCache(cacheKey, out var token))
            {
                return token;
            }

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (TryGetFromCache(cacheKey, out token))
                {
                    return token;
                }

                var authResult = await _tokenCredential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
                _cache.AddOrUpdate(cacheKey, authResult, (key, value) => authResult);
                return authResult;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
        }

        private static string GenerateCacheKey(TokenRequestContext requestContext)
        {
            return $"{string.Join(",", requestContext.Scopes)}";
        }

        private static bool IsAboutToExpire(AccessToken token)
        {
            var now = DateTimeOffset.UtcNow;
            return now >= token.ExpiresOn - s_tokenRefreshTime;
        }

        private bool TryGetFromCache(string key, out AccessToken value)
        {
            if (_cache.TryGetValue(key, out var token) && !IsAboutToExpire(token))
            {
                value = token;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
