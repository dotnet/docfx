// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class MicrosoftGraphConfig
    {
        /// <summary>
        /// Determines how long at most an alias remains valid in cache.
        /// </summary>
        public int MicrosoftGraphCacheExpirationInHours { get; private set; } = 30 * 24;

        /// <summary>
        /// Tenant id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphTenantId { get; private set; } = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        /// <summary>
        /// Client id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientId { get; private set; } = "b6b77d19-e9de-4611-bc6c-4f44640ec6fd";

        /// <summary>
        /// Client secret that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientSecret { get; private set; } = string.Empty;
    }
}
