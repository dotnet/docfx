// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class JsonSchemaConfig
    {
        /// <summary>
        /// Determines how long at most an alias remains valid in cache.
        /// </summary>
        public readonly int MicrosoftAliasCacheExpirationInHours = 30 * 24;
    }
}
