// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class CredentialHandler : DelegatingHandler
    {
        private readonly CredentialProvider _credentialProvider;

        public CredentialHandler(CredentialProvider credentialProvider)
#pragma warning disable CA2000 // Dispose objects before losing scope
            : this(credentialProvider, new HttpClientHandler())
#pragma warning restore CA2000 // Dispose objects before losing scope
        {
        }

        public CredentialHandler(CredentialProvider credentialProvider, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credentialProvider = credentialProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _credentialProvider.FillInCredentials(request);

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                && _credentialProvider.SupportRefreshToken)
            {
                await _credentialProvider.RefreshCredential(request);
                response = await base.SendAsync(request, cancellationToken);
            }
            return response;
        }
    }
}
