// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

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
        public string[] Docsets { get; private set; } = new[] { "**" };

        /// <summary>
        /// Gets the docset config file glob patterns.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Exclude { get; private set; } = Array.Empty<string>();
    }
}
