// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

#nullable enable

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
        public SourceInfo<string> Name { get; } = new SourceInfo<string>("");

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] Extend { get; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public Dictionary<string, HttpConfig> Http { get; } = new Dictionary<string, HttpConfig>();

        public Action<HttpRequestMessage> GetCredentialProvider()
        {
            var rules = Http.OrderByDescending(pair => pair.Key, StringComparer.Ordinal).ToArray();

            return message =>
            {
                var url = message.RequestUri.ToString();
                foreach (var (baseUrl, rule) in rules)
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
            };
        }
    }
}
