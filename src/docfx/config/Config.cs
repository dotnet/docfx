// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal sealed class Config
    {
        private static readonly string[] s_defaultInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultExclude = new[] { "_site/**/*", "localization/**/*" };

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public readonly string Product = string.Empty;

        /// <summary>
        /// Gets the default docset name
        /// </summary>
        public readonly string Name = string.Empty;

        /// <summary>
        /// Gets the file glob patterns included by the docset.
        /// </summary>
        public readonly string[] Files = s_defaultInclude;

        /// <summary>
        /// Gets the file glob patterns excluded from this docset.
        /// </summary>
        public readonly string[] Exclude = s_defaultExclude;

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public readonly OutputConfig Output = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly JObject GlobalMetadata = new JObject();

        /// <summary>
        /// The hostname
        /// </summary>
        public readonly string BaseUrl = string.Empty;

        /// <summary>
        /// The extend file addresses
        /// The addresses can be absolute url or relative path
        /// </summary>
        public readonly string[] Extend = Array.Empty<string>();

        /// <summary>
        /// Gets the file metadata added to each document.
        /// It is a map of `{metadata-name} -> {glob} -> {metadata-value}`
        /// </summary>
        public readonly Dictionary<string, Dictionary<string, JToken>> FileMetadata = new Dictionary<string, Dictionary<string, JToken>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public readonly Dictionary<string, string> Routes = new Dictionary<string, string>();

        /// <summary>
        /// Gets the configuration about contribution scenario.
        /// </summary>
        public readonly ContributionConfig Contribution = new ContributionConfig();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<string, string> Dependencies = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets the redirection mappings
        /// The default value is empty mappings
        /// The redirection always transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> Redirections = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets the redirection mappings without document id
        /// The default value is empty mappings
        /// The redirection doesn't transfer the document id
        /// </summary>
        public readonly Dictionary<string, string> RedirectionsWithoutId = new Dictionary<string, string>(PathUtility.PathComparer);

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public readonly DocumentIdConfig DocumentId = new DocumentIdConfig();

        /// <summary>
        /// Gets the rules for error levels by error code.
        /// </summary>
        public readonly Dictionary<string, ErrorLevel> Rules = new Dictionary<string, ErrorLevel>();

        /// <summary>
        /// Gets the authorization keys for required resources access
        /// </summary>
        public readonly Dictionary<string, HttpConfig> Http = new Dictionary<string, HttpConfig>();

        /// <summary>
        /// Gets the configurations related to GitHub APIs, usually related to resolve contributors.
        /// </summary>
        public readonly GitHubConfig GitHub = new GitHubConfig();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        public readonly string[] Xref = Array.Empty<string>();

        /// <summary>
        /// The configurations for localization build
        /// </summary>
        public readonly LocalizationConfig Localization = new LocalizationConfig();

        /// <summary>
        /// Gets the moniker range mapping
        /// </summary>
        public readonly Dictionary<string, string> MonikerRange = new Dictionary<string, string>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public readonly string MonikerDefinition = string.Empty;

        /// <summary>
        /// Get the theme repo url like https://github.com/docs/theme#master
        /// It's used for legacy doc(docs.com) sites build
        /// </summary>
        public readonly string Theme = string.Empty;

        /// <summary>
        /// Gets the config file name.
        /// </summary>
        [JsonIgnore]
        public string ConfigFileName { get; set; } = "docfx.yml";
    }
}
