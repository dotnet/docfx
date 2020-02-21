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
        public static readonly string[] DefaultInclude = new[] { "**/*.{md,yml,json}" };
        public static readonly string[] DefaultExclude = new[]
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
        public readonly string SiteName = "Docs";

        /// <summary>
        /// Gets the default product name
        /// </summary>
        public readonly string Product = "";

        /// <summary>
        /// Gets the file glob patterns included by the docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Files = DefaultInclude;

        /// <summary>
        /// Gets the file glob patterns excluded from this docset.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly string[] Exclude = Array.Empty<string>();

        /// <summary>
        /// Gets content build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly FileMappingConfig[] Content = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets resource build scope config for v2 backward compatibility.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly FileMappingConfig[] Resource = Array.Empty<FileMappingConfig>();

        /// <summary>
        /// Gets moniker range group configuration for v2 backward compatibility.
        /// </summary>
        public readonly Dictionary<string, GroupConfig> Groups = new Dictionary<string, GroupConfig>();

        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public readonly string OutputPath = "_site";

        /// <summary>
        /// Gets whether to output JSON model.
        /// </summary>
        public readonly bool OutputJson = false;

        /// <summary>
        /// For backward compatibility.
        /// Gets whether to generate `_op_pdfUrlPrefixTemplate` property in legacy metadata conversion.
        /// Front-end will display `Download PDF` link if `_op_pdfUrlPrefixTemplate` property is set.
        /// </summary>
        public readonly bool OutputPdf = false;

        /// <summary>
        /// Gets whether to use ugly url or pretty url when <see cref="Json"/> is set to false.
        ///  - Pretty url:      a.md --> a/index.html
        ///  - Ugly url:        a.md --> a.html
        /// </summary>
        public readonly bool UglifyUrl = false;

        /// <summary>
        /// Gets whether to lowercase all URLs and output file path.
        /// </summary>
        public readonly bool LowerCaseUrl = true;

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public readonly bool CopyResources = false;

        /// <summary>
        /// Gets the maximum errors to output.
        /// </summary>
        public readonly int MaxErrors = 1000;

        /// <summary>
        /// Gets the maximum warnings to output.
        /// </summary>
        public readonly int MaxWarnings = 1000;

        /// <summary>
        /// Gets the maximum suggestions to output.
        /// There are may be too many suggestion messages so increase the limit.
        /// </summary>
        public readonly int MaxSuggestions = 10000;

        /// <summary>
        /// Gets the global metadata added to each document.
        /// </summary>
        public readonly GlobalMetadata GlobalMetadata = new GlobalMetadata();

        /// <summary>
        /// Gets the {Schema}://{HostName}
        /// </summary>
        public string HostName { get; private set; } = "";

        /// <summary>
        /// Gets the site base path.
        /// </summary>
        public BasePath BasePath { get; private set; } = new BasePath("/");

        /// <summary>
        /// Gets host name used for generating .xrefmap.json
        /// </summary>
        public string XrefHostName { get; private set; } = "";

        /// <summary>
        /// Gets whether we are running in legacy mode
        /// </summary>
        public readonly bool Legacy;

        /// <summary>
        /// Gets whether we are running in dry run mode
        /// </summary>
        public readonly bool DryRun;

        /// <summary>
        /// Gets the file metadata added to each document.
        /// It is a map of `{metadata-name} -> {glob} -> {metadata-value}`
        /// </summary>
        public readonly Dictionary<string, SourceInfo<Dictionary<string, JToken>>> FileMetadata = new Dictionary<string, SourceInfo<Dictionary<string, JToken>>>();

        /// <summary>
        /// Gets a map from source folder path and output URL path.
        /// We rely on a Dictionary behavior that the enumeration order is the same as insertion order if there is no other mutations.
        /// </summary>
        public readonly Dictionary<PathString, PathString> Routes = new Dictionary<PathString, PathString>();

        /// <summary>
        /// Specify the repository url for contribution
        /// </summary>
        public readonly string? EditRepositoryUrl;

        /// <summary>
        /// Specify the repository branch for contribution
        /// </summary>
        public readonly string? EditRepositoryBranch;

        /// <summary>
        /// The excluded contributors which you don't want to show
        /// </summary>
        public readonly HashSet<string> ExcludeContributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the map from dependency name to git url
        /// All dependencies need to be restored locally before build
        /// The default value is empty mappings
        /// </summary>
        public readonly Dictionary<PathString, DependencyConfig> Dependencies = new Dictionary<PathString, DependencyConfig>();

        /// <summary>
        /// Gets the document id configuration section
        /// </summary>
        public readonly Dictionary<PathString, DocumentIdConfig> DocumentId = new Dictionary<PathString, DocumentIdConfig>();

        /// <summary>
        /// Gets allow custom error code, severity and message.
        /// </summary>
        public readonly Dictionary<string, CustomError> CustomErrors = new Dictionary<string, CustomError>();

        /// <summary>
        /// Gets whether warnings should be treated as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        /// <summary>
        /// The addresses of xref map files, used for resolving xref.
        /// They should be absolute url or relative path
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly SourceInfo<string>[] Xref = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Gets the moniker range mapping
        /// </summary>
        public readonly Dictionary<string, SourceInfo<string>> MonikerRange = new Dictionary<string, SourceInfo<string>>();

        /// <summary>
        /// Get the definition of monikers
        /// It should be absolute url or relative path
        /// </summary>
        public readonly SourceInfo<string> MonikerDefinition = new SourceInfo<string>("");

        /// <summary>
        /// Get the file path of content validation rules
        /// </summary>
        public readonly SourceInfo<string> MarkdownValidationRules = new SourceInfo<string>("");

        /// <summary>
        /// Get the metadata JSON schema file path.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public readonly SourceInfo<string>[] MetadataSchema = Array.Empty<SourceInfo<string>>();

        /// <summary>
        /// Get the template folder or git repository url (like https://github.com/docs/theme#master)
        /// </summary>
        public readonly PackagePath Template = new PackagePath();

        /// <summary>
        /// When enabled, updated_at for each document will be the last build time
        /// for the latest commit that touches that document.
        /// </summary>
        public readonly bool UpdateTimeAsCommitBuildTime = false;

        /// <summary>
        /// When enabled, update the state of commit build time for this build.
        /// </summary>
        public readonly bool UpdateCommitBuildTime = true;

        /// <summary>
        /// Token that can be used to access the GitHub API.
        /// </summary>
        public readonly string GithubToken = "";

        /// <summary>
        /// Determines how long at most a user remains valid in cache.
        /// </summary>
        public readonly int GithubUserCacheExpirationInHours = 30 * 24;

        /// <summary>
        /// Determines whether to resolve git commit user and GitHub user.
        /// We only resolve github user when an <see cref="GithubToken"/> is provided.
        /// </summary>
        public readonly bool ResolveGithubUsers = true;

        /// <summary>
        /// Determines how long at most an alias remains valid in cache.
        /// </summary>
        public readonly int MicrosoftGraphCacheExpirationInHours = 30 * 24;

        /// <summary>
        /// Tenant id that can be used to access the Microsoft Graph API.
        /// </summary>
        public readonly string MicrosoftGraphTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        /// <summary>
        /// Client id that can be used to access the Microsoft Graph API.
        /// </summary>
        public readonly string MicrosoftGraphClientId = "b6b77d19-e9de-4611-bc6c-4f44640ec6fd";

        /// <summary>
        /// Client secret that can be used to access the Microsoft Graph API.
        /// </summary>
        public readonly string MicrosoftGraphClientSecret = "";

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
            if (LowerCaseUrl)
            {
                HostName = HostName.ToLowerInvariant();
                BasePath = new BasePath(BasePath.Original.ToLowerInvariant());
            }

            DefaultLocale = DefaultLocale.ToLowerInvariant();
        }
    }
}
