// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Microsoft.Docs.Build;

internal class MicrosoftGraphAuthenticationProvider : IAuthenticationProvider, IDisposable
{
    private static readonly string[] s_scopes = { "https://graph.microsoft.com/.default" };

    private readonly X509Certificate2 _clientCertificate;
    private readonly IConfidentialClientApplication _clientApp;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private AuthenticationResult? _authenticationResult;

    public MicrosoftGraphAuthenticationProvider(string tenantId, string clientId, string clientCertificate)
    {
        _clientCertificate = new X509Certificate2(
            Convert.FromBase64String(clientCertificate),
            password: "",
            X509KeyStorageFlags.EphemeralKeySet);
        _clientApp = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithCertificate(_clientCertificate)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
            .WithRedirectUri("https://www.microsoft.com")
            .Build();
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        var accessToken = await GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
    }

    public void Dispose()
    {
        _clientCertificate.Dispose();
        _semaphore.Dispose();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            if (_authenticationResult == null || DateTime.UtcNow > _authenticationResult.ExpiresOn.UtcDateTime.AddMinutes(-1))
            {
                _authenticationResult = await _clientApp.AcquireTokenForClient(s_scopes).WithSendX5C(true).ExecuteAsync();
            }
            return _authenticationResult.AccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
