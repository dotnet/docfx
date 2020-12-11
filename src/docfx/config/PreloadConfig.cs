// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public SourceInfo<string> Name { get; private set; } = new SourceInfo<string>("");

        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public string OutputPath { get; private set; } = "_site";

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public string DefaultLocale { get; private set; } = "en-us";

        /// <summary>
        /// Specify the fallback repository url.
        /// For localization build, it takes precedence over convention-calculated url.
        /// </summary>
        public PackagePath? FallbackRepository { get; private set; }

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] Extend { get; private set; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public Dictionary<string, HttpConfig> Http { get; private set; } = new Dictionary<string, HttpConfig>();

        /// <summary>
        /// Type of git access token used to access the GitHub API
        /// </summary>
        public DocsGitTokenType? DocsGitTokenType { get; private set; }

        /// <summary>
        /// Name of the git repository owner
        /// </summary>
        public string? DocsRepositoryOwnerName { get; private set; }

        public Action<HttpRequestMessage> GetCredentialProvider()
        {
            var rules = Http.OrderByDescending(pair => pair.Key, StringComparer.Ordinal).ToArray();

            return message =>
            {
                if (message.RequestUri?.ToString() is string url)
                {
                    foreach (var (baseUrl, rule) in rules)
                    {
                        if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var header in rule.Headers)
                            {
                                message.Headers.Add(header.Key, header.Value);
                            }
                            break;
                        }
                    }
                }
            };
        }
    }
}
