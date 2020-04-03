// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// Gets the default site name
        /// </summary>
        public string SiteName { get; private set; } = "Docs";

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public string Product { get; private set; } = "";

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
        public Dictionary<string, GroupConfig> Groups { get; } = new Dictionary<string, GroupConfig>();

        /// <summary>
        /// Gets whether to output JSON model.
        /// </summary>
        public bool OutputJson { get; private set; } = false;

        /// <summary>
        /// For backward compatibility.
        /// Gets whether to generate `_op_pdfUrlPrefixTemplate` property in legacy metadata conversion.
        /// Front-end will display `Download PDF` link if `_op_pdfUrlPrefixTemplate` property is set.
        /// </summary>
        public bool OutputPdf { get; private set; } = false;

        /// <summary>
        /// Gets whether to use ugly url or pretty url when <see cref="Json"/> is set to false.
        ///  - Pretty url:      a.md --> a/index.html
        ///  - Ugly url:        a.md --> a.html
        /// </summary>
        public bool UglifyUrl { get; private set; } = false;

        /// <summary>
        /// Gets whether to lowercase all URLs and output file path.
        /// </summary>
        public bool LowerCaseUrl { get; private set; } = true;

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public bool CopyResources { get; private set; } = false;

        /// <summary>
        /// Gets the maximum errors to output.
        /// </summary>
        public int MaxErrors { get; private set; } = 1000;

        /// <summary>
        /// Gets the maximum warnings to output.
        /// </summary>
        public int MaxWarnings { get; private set; } = 1000;

        /// <summary>
        /// Gets the maximum suggestions to output.
        /// There are may be too many suggestion messages so increase the limit.
        /// </summary>
        public int MaxSuggestions { get; private set; } = 10000;

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public GlobalMetadata GlobalMetadata { get; private set; } = new GlobalMetadata();

        /// <summary>
        /// Gets the {Schema}://{HostName}
        /// </summary>
        public string HostName { get; private set; } = "";

        /// <summary>
        /// Gets the site base path.
        /// </summary>
        public BasePath BasePath { get; private set; }

        /// <summary>
        /// Gets host name used for generating .xrefmap.json
        /// </summary>
        public string XrefHostName { get; private set; } = "";

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
        public Dictionary<string, SourceInfo<Dictionary<string, JToken>>> FileMetadata { get; } = new Dictionary<string, SourceInfo<Dictionary<string, JToken>>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public Dictionary<PathString, PathString> Routes { get; } = new Dictionary<PathString, PathString>();

        /// <summary>
        /// Specify the repository url for contribution
        /// </summary>
        public string? EditRepositoryUrl { get; private set; }

        /// <summary>
        /// Specify the repository branch for contribution
        /// </summary>
        public string? EditRepositoryBranch { get; private set; }

        /// <summary>
        /// The excluded contributors which you don't want to show
        /// </summary>
        public HashSet<string> ExcludeContributors { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public Dictionary<PathString, DependencyConfig> Dependencies { get; } = new Dictionary<PathString, DependencyConfig>();

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public Dictionary<PathString, DocumentIdConfig> DocumentId { get; } = new Dictionary<PathString, DocumentIdConfig>();

        /// <summary>
        /// Gets allow custom error code, severity and message.
        /// </summary>
        public Dictionary<string, CustomError> CustomErrors { get; } = new Dictionary<string, CustomError>();

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
        public Dictionary<string, SourceInfo<string?>> MonikerRange { get; } = new Dictionary<string, SourceInfo<string?>>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public SourceInfo<string> MonikerDefinition { get; private set; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of content validation rules
        /// </summary>
        public SourceInfo<string> MarkdownValidationRules { get; private set; } = new SourceInfo<string>("");

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

        /// <summary>
        /// Overwrite current <see cref="CommitBuildTimeProvider._buildTime"/>
        /// </summary>
        public DateTime? BuildTime { get; private set; }

        /// <summary>
        /// Token that can be used to access the GitHub API.
        /// </summary>
        public string GithubToken { get; private set; } = "";

        /// <summary>
        /// Determines how long at most a user remains valid in cache.
        /// </summary>
        public int GithubUserCacheExpirationInHours { get; private set; } = 30 * 24;

        /// <summary>
        /// Determines whether to resolve git commit user and GitHub user.
        /// We only resolve github user when an <see cref="GithubToken"/> is provided.
        /// </summary>
        public bool ResolveGithubUsers { get; private set; } = true;

        /// <summary>
        /// Determines how long at most an alias remains valid in cache.
        /// </summary>
        public int MicrosoftGraphCacheExpirationInHours { get; private set; } = 30 * 24;

        /// <summary>
        /// Tenant id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphTenantId { get; private set; } = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        /// <summary>
        /// Client id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientId { get; private set; } = "b6b77d19-e9de-4611-bc6c-4f44640ec6fd";

        /// <summary>
        /// Client secret that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientSecret { get; private set; } = "";

        /// <summary>
        /// A file containing a map of file path to the original file path.
        /// </summary>
        public SourceInfo<string> SourceMap { get; private set; } = new SourceInfo<string>("");

        /// <summary>
        /// Determines if validate the moniker confguration.
        /// </summary>
        public bool SkipMonikerValidation { get; private set; }

        public IEnumerable<SourceInfo<string>> GetFileReferences()
        {
            foreach (var url in Xref)
            {
                yield return url;
            }

            yield return SourceMap;
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
            if (LowerCaseUrl)
            {
                HostName = HostName.ToLowerInvariant();
                BasePath = new BasePath(BasePath.Value.ToLowerInvariant());
            }
        }
    }
}
