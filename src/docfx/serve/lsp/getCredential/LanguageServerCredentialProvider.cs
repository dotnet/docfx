// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerCredentialProvider
    {
        private readonly ILanguageServerFacade _languageServer;
        private readonly ILogger _logger;

        public LanguageServerCredentialProvider(ILoggerFactory loggerFactory, ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
            _logger = loggerFactory.CreateLogger<LanguageServerCredentialProvider>();
        }

        public async Task<Dictionary<string, HttpConfig>> GetCredential(string url)
        {
            try
            {
                var credentialRefreshResponse = await _languageServer.SendRequest(
                    new GetCredentialParams() { Url = url },
                    CancellationToken.None);
                return credentialRefreshResponse.Http;
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Get credential for '{url}' failed: {ex}");
                return new();
            }
        }
    }
}
