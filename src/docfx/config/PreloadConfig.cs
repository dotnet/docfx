// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
        public SourceInfo<string> Name { get; init; } = new SourceInfo<string>("");

        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public string OutputPath { get; init; } = "_site";

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public string DefaultLocale { get; init; } = "en-us";

        /// <summary>
        /// Specify the fallback repository url.
        /// For localization build, it takes precedence over convention-calculated url.
        /// </summary>
        public PackagePath? FallbackRepository { get; init; }

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] Extend { get; init; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public Dictionary<string, HttpConfig> Http { get; init; } = new Dictionary<string, HttpConfig>();

        /// <summary>
        /// Type of git access token used to access the GitHub API
        /// </summary>
        public DocsGitTokenType? DocsGitTokenType { get; init; }

        /// <summary>
        /// Name of the git repository owner
        /// </summary>
        public string? DocsRepositoryOwnerName { get; init; }

        public Dictionary<string, HttpConfig> GetHttpConfig()
            => Http.OrderByDescending(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);
    }
}
