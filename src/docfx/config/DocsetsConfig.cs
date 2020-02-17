// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Contains location of docsets in a multiple docsets setup
    /// </summary>
    internal class DocsetsConfig
    {
        /// <summary>
        /// Gets the docset config file glob pattern.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Docsets = new[] { "**" };

        /// <summary>
        /// Gets the docset config file glob patterns.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Exclude = Array.Empty<string>();
    }
}
