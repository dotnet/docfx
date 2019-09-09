// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal sealed class FileGroupConfig
    {
        /// <summary>
        /// Gets the file glob patterns included by the group.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Files = Array.Empty<string>();

        /// <summary>
        /// Gets the file glob patterns excluded from the group.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Exclude = Array.Empty<string>();
    }
}
