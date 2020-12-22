// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerCredentialRefresher
    {
        private readonly ILanguageServerFacade _languageServer;

        public LanguageServerCredentialRefresher(ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<string?> GetRefreshedToken(CancellationToken cancellationToken)
        {
            var credentialRefreshResponse = await _languageServer.SendRequest(
                new CredentialRefreshParams() { Type = CredentialType.DocsOpsToken }, cancellationToken);

            return "";

            // return credentialRefreshResponse?.Token;
        }
    }
}
