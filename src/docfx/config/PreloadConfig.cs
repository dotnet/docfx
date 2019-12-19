// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Contains config values that MUST exist before loading the full configuration.
    /// </summary>
    internal class PreloadConfig
    {
        /// <summary>
        /// Gets the default docset name
        /// </summary>
        public readonly SourceInfo<string> Name = new SourceInfo<string>("");

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly SourceInfo<string>[] Extend = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public readonly Dictionary<string, HttpConfig> Http = new Dictionary<string, HttpConfig>();

        public void ProvideCredential(HttpRequestMessage message)
        {
            var url = message.RequestUri.ToString();
            foreach (var (baseUrl, rule) in Http)
            {
                if (url.StartsWith(baseUrl))
                {
                    foreach (var header in rule.Headers)
                    {
                        message.Headers.Add(header.Key, header.Value);
                    }
                    break;
                }
            }
        }
    }
}
