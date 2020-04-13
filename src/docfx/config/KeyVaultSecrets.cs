// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Docs.Build
{
    internal static class KeyVaultSecrets
    {
        private static readonly string s_keyVaultEndPoint = OpsConfigAdapter.DocsEnvironment switch
        {
            DocsEnvironment.Prod => "https://kv-docs-build-prod.vault.azure.net",
            DocsEnvironment.PPE => "https://kv-docs-build-sandbox.vault.azure.net",
            DocsEnvironment.Internal => "https://kv-docs-build-internal.vault.azure.net",
            DocsEnvironment.Perf => "https://kv-docs-build-perf.vault.azure.net",
            _ => throw new NotSupportedException(),
        };

        private static readonly Lazy<SecretClient> s_secretClient = new Lazy<SecretClient>(()
            => new SecretClient(new Uri(s_keyVaultEndPoint), new DefaultAzureCredential()));

        public static Lazy<Task<Response<KeyVaultSecret>>> OPBuildUserToken { get; } = GetSecret("opBuildUserToken");

        private static Lazy<Task<Response<KeyVaultSecret>>> GetSecret(string key)
        {
            return new Lazy<Task<Response<KeyVaultSecret>>>(() => s_secretClient.Value.GetSecretAsync(key));
        }
    }
}
