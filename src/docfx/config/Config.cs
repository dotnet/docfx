// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class Config : PreloadConfig
    {
        public static string[] DefaultInclude => new[] { "**/*.{md,yml,json}" };

        public static string[] DefaultExclude => new[]
        {
            "_site/**",             // Default output location
            "_localization/**",     // Localization file when using folder convention
            "_themes/**",           // Default template location
        };

        /// <summary>
        /// Gets the default locale of this docset.
        /// </summary>
        public string DefaultLocale { get; private set; } = "en-us";

        /// <summary>
        /// Gets the default site name
        /// </summary>
        public string SiteName { get; private set; } = "Docs";

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public string Product { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the file glob patterns included by the docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Files { get; private set; } = DefaultInclude;

        /// <summary>
        /// Gets the file glob patterns excluded from this docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Exclude { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Gets content build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public FileMappingConfig[] Content { get; private set; } = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets resource build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public FileMappingConfig[] Resource { get; private set; } = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets moniker range group configuration for v2 backward compatibility.
        /// </summary>
        public Dictionary<string, GroupConfig> Groups { get; private set; } = new Dictionary<string, GroupConfig>();

        /// <summary>
        /// Gets the output config.
        /// </summary>
        public OutputConfig Output { get; private set; } = new OutputConfig();

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public GlobalMetadata GlobalMetadata { get; private set; } = new GlobalMetadata();

        /// <summary>
        /// Gets the {Schema}://{HostName}
        /// </summary>
        public string HostName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the site base path.
        /// </summary>
        public BasePath BasePath { get; private set; } = new BasePath("/");

        /// <summary>
        /// Gets host name used for generating .xrefmap.json
        /// </summary>
        public string XrefHostName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets whether we are running in legacy mode
        /// </summary>
        public bool Legacy { get; private set; }

        /// <summary>
        /// Gets whether we are running in dry run mode
        /// </summary>
        public bool DryRun { get; private set; }

        /// <summary>
        /// Gets the file metadata added to each document.
        /// It is a map of `{metadata-name} -> {glob} -> {metadata-value}`
        /// </summary>
        public Dictionary<string, SourceInfo<Dictionary<string, JToken>>> FileMetadata { get; private set; } = new Dictionary<string, SourceInfo<Dictionary<string, JToken>>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public Dictionary<PathString, PathString> Routes { get; private set; } = new Dictionary<PathString, PathString>();

        /// <summary>
        /// Gets the configuration about contribution scenario.
        /// </summary>
        public ContributionConfig Contribution { get; private set; } = new ContributionConfig();

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public Dictionary<PathString, DependencyConfig> Dependencies { get; private set; } = new Dictionary<PathString, DependencyConfig>();

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public Dictionary<PathString, DocumentIdConfig> DocumentId { get; private set; } = new Dictionary<PathString, DocumentIdConfig>();

        /// <summary>
        /// Gets allow custom error code, severity and message.
        /// </summary>
        public Dictionary<string, CustomError> CustomErrors { get; private set; } = new Dictionary<string, CustomError>();

        /// <summary>
        /// Gets the configurations related to GitHub APIs, usually related to resolve contributors.
        /// </summary>
        public GitHubConfig GitHub { get; private set; } = new GitHubConfig();

        /// <summary>
        /// Gets the configurations related to Microsoft Graph.
        /// </summary>
        public MicrosoftGraphConfig MicrosoftGraph { get; private set; } = new MicrosoftGraphConfig();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public bool WarningsAsErrors { get; private set; }

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] Xref { get; private set; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the moniker range mapping
        /// </summary>
        public Dictionary<string, SourceInfo<string>> MonikerRange { get; private set; } = new Dictionary<string, SourceInfo<string>>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public SourceInfo<string> MonikerDefinition { get; private set; } = new SourceInfo<string>(string.Empty);

        /// <summary>
        /// Get the file path of content validation rules
        /// </summary>
        public SourceInfo<string> MarkdownValidationRules { get; private set; } = new SourceInfo<string>(string.Empty);

        /// <summary>
        /// Get the metadata JSON schema file path.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] MetadataSchema { get; private set; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Get the template folder or git repository url (like https://github.com/docs/theme#master)
        /// </summary>
        public PackagePath Template { get; private set; } = new PackagePath();

        /// <summary>
        /// When enabled, updated_at for each document will be the last build time
        /// for the latest commit that touches that document.
        /// </summary>
        public bool UpdateTimeAsCommitBuildTime { get; private set; } = false;

        /// <summary>
        /// When enabled, update the state of commit build time for this build.
        /// </summary>
        public bool UpdateCommitBuildTime { get; private set; } = true;

        public IEnumerable<SourceInfo<string>> GetFileReferences()
        {
            foreach (var url in Xref)
            {
                yield return url;
            }

            yield return MonikerDefinition;
            yield return MarkdownValidationRules;

            foreach (var metadataSchema in MetadataSchema)
            {
                yield return metadataSchema;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Output.LowerCaseUrl)
            {
                HostName = HostName.ToLowerInvariant();
                BasePath = new BasePath(BasePath.Original.ToLowerInvariant());
            }

            DefaultLocale = DefaultLocale.ToLowerInvariant();
        }
    }
}
