// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerCredentialRefresher
    {
        private readonly ILanguageServerFacade _languageServer;
        private readonly ILogger _logger;

        public LanguageServerCredentialRefresher(ILoggerFactory loggerFactory, ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
            _logger = loggerFactory.CreateLogger<LanguageServerCredentialRefresher>();
        }

        public async Task<string?> GetRefreshedToken(CancellationToken cancellationToken)
        {
            var credentialRefreshResponse = await _languageServer.SendRequest(
                new CredentialRefreshParams() { Type = CredentialType.DocsOpsToken },
                cancellationToken);
            if (credentialRefreshResponse.Error != null || credentialRefreshResponse.Result?.Token == null)
            {
                _logger.LogCritical($"Failed to refresh OP Build User token: {credentialRefreshResponse.Error?.Message}");
                return default;
            }

            return credentialRefreshResponse.Result.Token;
        }
    }
}
