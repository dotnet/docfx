// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Microsoft.Docs.Build
{
    internal static class KeyVaultSecret
    {
        private static readonly Lazy<KeyVaultClient> s_keyVaultClient = new Lazy<KeyVaultClient>(() => new KeyVaultClient(
            new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)));

        private static readonly string s_keyVaultEndPoint = OpsConfigAdapter.DocsEnvironment switch
        {
            DocsEnvironment.Prod => "https://kv-docs-build-prod.vault.azure.net",
            DocsEnvironment.PPE => "https://kv-docs-build-sandbox.vault.azure.net",
            DocsEnvironment.Internal => "https://kv-docs-build-internal.vault.azure.net",
            DocsEnvironment.Perf => "https://kv-docs-build-perf.vault.azure.net",
            _ => throw new NotSupportedException(),
        };

        public static Lazy<string> OPBuildUserToken => GetSecret("opBuildUserToken");

        private static Lazy<string> GetSecret(string key)
        {
            return new Lazy<string>(() => s_keyVaultClient.Value.GetSecretAsync($"{s_keyVaultEndPoint}/secrets/{key}").Result.Value);
        }
    }
}
