// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ECMA2Yaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Config : PreloadConfig
    {
        public static string[] DefaultInclude => new[] { "**" };

        public static string[] DefaultExclude => new[]
        {
            "_site/**",             // Default output location
            "_localization/**",     // Localization file when using folder convention
            "_themes/**",           // Default template location
            "_themes.pdf/**",       // Default PDF template location
        };

        /// <summary>
        /// Gets the default site name
        /// </summary>
        public string SiteName { get; init; } = "Docs";

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public string Product { get; init; } = "";

        /// <summary>
        /// Gets the file glob patterns included by the docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Files { get; init; } = DefaultInclude;

        /// <summary>
        /// Gets the file glob patterns excluded from this docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[] Exclude { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets content build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public FileMappingConfig[] Content { get; init; } = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets resource build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public FileMappingConfig[] Resource { get; init; } = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets moniker range group configuration for v2 backward compatibility.
        /// </summary>
        public Dictionary<string, GroupConfig> Groups { get; } = new Dictionary<string, GroupConfig>();

        /// <summary>
        /// Gets output file type
        /// </summary>
        public OutputType OutputType { get; init; } = OutputType.Html;

        /// <summary>
        /// For backward compatibility.
        /// Gets whether to generate `_op_pdfUrlPrefixTemplate` property in legacy metadata conversion.
        /// Front-end will display `Download PDF` link if `_op_pdfUrlPrefixTemplate` property is set.
        /// </summary>
        public bool OutputPdf { get; init; }

        /// <summary>
        /// Gets Output Url type
        /// </summary>
        public UrlType UrlType { get; init; } = UrlType.Pretty;

        /// <summary>
        /// Gets whether to lowercase all URLs and output file path.
        /// </summary>
        public bool LowerCaseUrl { get; init; } = true;

        /// <summary>
        /// Gets whether dependencies such as images and template styles
        /// are copied to output so the output folder can be deployed as a self-contained website.
        /// </summary>
        public bool SelfContained { get; init; } = true;

        /// <summary>
        /// Gets the maximum errors of each file to output.
        /// </summary>
        public int MaxFileErrors { get; init; } = 100;

        /// <summary>
        /// Gets the maximum warnings of each file to output.
        /// </summary>
        public int MaxFileWarnings { get; init; } = 1000;

        /// <summary>
        /// Gets the maximum suggestions of each file to output.
        /// </summary>
        public int MaxFileSuggestions { get; init; } = 1000;

        /// <summary>
        /// Gets the maximum info of each file to output.
        /// </summary>
        public int MaxFileInfos { get; init; } = 20;

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public GlobalMetadata GlobalMetadata { get; init; } = new GlobalMetadata();

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
        public string XrefHostName { get; init; } = "";

        /// <summary>
        /// Gets whether we are running in dry run mode
        /// </summary>
        public bool DryRun { get; init; }

        /// <summary>
        /// Gets the file metadata added to each document.
        /// It is a map of `{metadata-name} -> {glob} -> {metadata-value}`
        /// </summary>
        public Dictionary<string, SourceInfo<Dictionary<string, JToken>>> FileMetadata { get; } =
            new Dictionary<string, SourceInfo<Dictionary<string, JToken>>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public Dictionary<PathString, PathString> Routes { get; } = new Dictionary<PathString, PathString>();

        /// <summary>
        /// Specify the repository url for contribution
        /// </summary>
        public string? EditRepositoryUrl { get; init; }

        /// <summary>
        /// Specify the repository branch for contribution
        /// </summary>
        public string? EditRepositoryBranch { get; init; }

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
        public Dictionary<string, SourceInfo<CustomRule>> Rules { get; } = new Dictionary<string, SourceInfo<CustomRule>>();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public bool WarningsAsErrors { get; init; }

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] Xref { get; init; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the moniker range mapping
        /// </summary>
        public Dictionary<string, SourceInfo<string?>> MonikerRange { get; } = new Dictionary<string, SourceInfo<string?>>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public SourceInfo<string> MonikerDefinition { get; init; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of content validation rules
        /// </summary>
        public SourceInfo<string> MarkdownValidationRules { get; init; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of Azure SandboxEnabledModuleList
        /// </summary>
        public SourceInfo<string> SandboxEnabledModuleList { get; private set; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of build validation rules
        /// </summary>
        public SourceInfo<string> BuildValidationRules { get; init; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of allow lists
        /// </summary>
        public SourceInfo<string> Allowlists { get; init; } = new SourceInfo<string>("");

        /// <summary>
        /// Get the metadata JSON schema file path.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] MetadataSchema { get; init; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Get the template folder or git repository url (like https://github.com/docs/theme#master)
        /// </summary>
        public PackagePath Template { get; init; } = new PackagePath();

        /// <summary>
        /// Get the template base path used for referencing the template resource file when apply liquid.
        /// If not provided, referencing to template resource file will be resolved to physical absolute path.
        /// </summary>
        public string? TemplateBasePath { get; init; }

        /// <summary>
        /// Gets the search index type like [lunr](https://lunrjs.com/)
        /// </summary>
        public SearchEngineType SearchEngine { get; init; }

        /// <summary>
        /// When enabled, updated_at for each document will be the last build time
        /// for the latest commit that touches that document.
        /// </summary>
        public bool UpdateTimeAsCommitBuildTime { get; init; }

        /// <summary>
        /// When enabled, update the state of commit build time for this build.
        /// </summary>
        public bool UpdateCommitBuildTime { get; init; } = true;

        /// <summary>
        /// Overwrite current <see cref="CommitBuildTimeProvider._buildTime"/>
        /// </summary>
        public DateTime? BuildTime { get; init; }

        /// <summary>
        /// Token that can be used to access the GitHub API.
        /// </summary>
        public string GithubToken { get; init; } = "";

        /// <summary>
        /// Determines how long at most a user remains valid in cache.
        /// </summary>
        public int GithubUserCacheExpirationInHours { get; init; } = 30 * 24;

        /// <summary>
        /// Determines whether to resolve git commit user and GitHub user.
        /// We only resolve github user when an <see cref="GithubToken"/> is provided.
        /// </summary>
        public bool ResolveGithubUsers { get; init; } = true;

        /// <summary>
        /// Determines how long at most an alias remains valid in cache.
        /// </summary>
        public int MicrosoftGraphCacheExpirationInHours { get; init; } = 30 * 24;

        /// <summary>
        /// Tenant id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphTenantId { get; init; } = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        /// <summary>
        /// Client id that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientId { get; init; } = "b6b77d19-e9de-4611-bc6c-4f44640ec6fd";

        /// <summary>
        /// The base64 encoded client cert that can be used to access the Microsoft Graph API.
        /// </summary>
        public string MicrosoftGraphClientCertificate { get; init; } = "";

        /// <summary>
        /// A file containing a map of file path to the original file path.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[] SourceMap { get; init; } = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Determines if remove host name
        /// </summary>
        public bool RemoveHostName { get; init; }

        /// <summary>
        /// Determines if run learn-validation as post process
        /// </summary>
        public bool RunLearnValidation { get; init; }

        /// <summary>
        /// Determines if disable dry sync
        /// </summary>
        public bool NoDrySync { get; init; }

        /// <summary>
        /// Determines and configures build to consume XML files produced from monodoc
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public ECMA2YamlRepoConfig[]? Monodoc { get; init; }

        /// <summary>
        /// Determines and configures build to convert MAML markdown files to SDP yaml files as input
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public string[]? MAMLMonikerPath { get; init; }

        public JoinTOCConfig[] JoinTOC { get; init; } = Array.Empty<JoinTOCConfig>();

        public HashSet<PathString> SplitTOC { get; init; } = new HashSet<PathString>();

        public HashSet<string> RedirectionFiles { get; init; } = new HashSet<string>();

        public IEnumerable<SourceInfo<string>> GetFileReferences()
        {
            foreach (var url in Xref)
            {
                yield return url;
            }

            foreach (var sourceMap in SourceMap)
            {
                yield return sourceMap;
            }
            yield return MonikerDefinition;
            yield return MarkdownValidationRules;
            yield return BuildValidationRules;
            yield return Allowlists;
            yield return SandboxEnabledModuleList;

            foreach (var metadataSchema in MetadataSchema)
            {
                yield return metadataSchema;
            }

            foreach (var item in JoinTOC)
            {
                if (item.TopLevelToc != null)
                {
                    yield return new SourceInfo<string>(item.TopLevelToc);
                }
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
