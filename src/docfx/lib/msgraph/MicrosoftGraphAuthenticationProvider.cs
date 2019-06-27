// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphAuthenticationProvider : IAuthenticationProvider, IDisposable
    {
        private static string resource = "https://graph.microsoft.com/.default";
        private static string[] scopes = new string[] { resource };

        private readonly ConfidentialClientApplication cca;
        private readonly AuthenticationResult authenticationResult;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public MicrosoftGraphAuthenticationProvider(string tenantId, string clientId, string clientSecret)
        {
            var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            this.cca = new ConfidentialClientApplication(clientId, authority, "http://www.microsoft.com", new ClientCredential(clientSecret), null, null);
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var accessToken = await GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                await semaphore.WaitAsync();
                if (authenticationResult == null || authenticationResult.ExpiresOn.UtcDateTime < DateTime.UtcNow)
                {
                    authenticationResult = await cca.AcquireTokenForClientAsync(scopes);
                }
                return authenticationResult.AccessToken;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
