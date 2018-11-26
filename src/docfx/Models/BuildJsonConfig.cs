// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [Serializable]
    public class BuildJsonConfig
    {
        [JsonIgnore]
        private Dictionary<string, GroupConfig> _versions;

        [JsonIgnore]
        private Dictionary<string, GroupConfig> _groups;

        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonIgnore]
        public string OutputFolder { get; set; }

        [JsonProperty("content")]
        public FileMapping Content { get; set; }

        [JsonProperty("resource")]
        public FileMapping Resource { get; set; }

        [JsonProperty("overwrite")]
        public FileMapping Overwrite { get; set; }

        [JsonProperty("pairing")]
        public List<ContentPairingInfo> Pairing { get; set; }

        [JsonProperty("xrefTags")]
        public ListWithStringFallback XrefTags { get; set; }

        [JsonProperty("xref")]
        public ListWithStringFallback XRefMaps { get; set; }

        [JsonProperty("xrefService")]
        public ListWithStringFallback XRefServiceUrls { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("globalMetadata")]
        [JsonConverter(typeof(JObjectDictionaryToObjectDictionaryConverter))]
        public Dictionary<string, object> GlobalMetadata { get; set; }

        [JsonProperty("globalMetadataFiles")]
        public ListWithStringFallback GlobalMetadataFilePaths { get; set; } = new ListWithStringFallback();

        /// <summary>
        /// Metadata that applies to some specific files.
        /// The key is the metadata name.
        /// For each item of the value:
        ///     The key is the glob pattern to match the files.
        ///     The value is the value of the metadata.
        /// </summary>
        [JsonProperty("fileMetadata")]
        public Dictionary<string, FileMetadataPairs> FileMetadata { get; set; }

        [JsonProperty("tagParameters")]
        public Dictionary<string, JArray> TagParameters { get; set; }

        [JsonProperty("fileMetadataFiles")]
        public ListWithStringFallback FileMetadataFilePaths { get; set; } = new ListWithStringFallback();

        [JsonProperty("template")]
        public ListWithStringFallback Templates { get; set; } = new ListWithStringFallback();

        [JsonProperty("theme")]
        public ListWithStringFallback Themes { get; set; }

        [JsonProperty("postProcessors")]
        public ListWithStringFallback PostProcessors { get; set; } = new ListWithStringFallback();

        [JsonProperty("serve")]
        public bool? Serve { get; set; }

        [JsonProperty("force")]
        public bool? Force { get; set; }

        [JsonProperty("debug")]
        public bool? EnableDebugMode { get; set; }

        [JsonProperty("debugOutput")]
        public string OutputFolderForDebugFiles { get; set; }

        [JsonProperty("forcePostProcess")]
        public bool? ForcePostProcess { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("exportRawModel")]
        public bool? ExportRawModel { get; set; }

        [JsonProperty("rawModelOutputFolder")]
        public string RawModelOutputFolder { get; set; }

        [JsonProperty("exportViewModel")]
        public bool? ExportViewModel { get; set; }

        [JsonProperty("viewModelOutputFolder")]
        public string ViewModelOutputFolder { get; set; }

        [JsonProperty("dryRun")]
        public bool? DryRun { get; set; }

        [JsonProperty("maxParallelism")]
        public int? MaxParallelism { get; set; }

        [JsonProperty("markdownEngineName")]
        public string MarkdownEngineName { get; set; }

        [JsonProperty("markdownEngineProperties")]
        [JsonConverter(typeof(JObjectDictionaryToObjectDictionaryConverter))]
        public Dictionary<string, object> MarkdownEngineProperties { get; set; }

        /// <summary>
        /// Disable default lang keyword, e.g. <see langword="null"/>
        /// </summary>
        [JsonProperty("noLangKeyword")]
        public bool NoLangKeyword { get; set; }

        [JsonProperty("intermediateFolder")]
        public string IntermediateFolder { get; set; }

        [JsonProperty("changesFile")]
        public string ChangesFile { get; set; }

        [JsonProperty("customLinkResolver")]
        public string CustomLinkResolver { get; set; }

        [Obsolete]
        [JsonProperty("versions")]
        public Dictionary<string, GroupConfig> Versions
        {
            get
            {
                return _versions;
            }
            set
            {
                _versions = value;
            }
        }

        [JsonProperty("groups")]
        public Dictionary<string, GroupConfig> Groups
        {
            get
            {
                return _groups ?? _versions;
            }
            set
            {
                _groups = value;
                _versions = value;
            }

        }

        [JsonProperty("lruSize")]
        public int? LruSize { get; set; }

        [JsonProperty("keepFileLink")]
        public bool KeepFileLink { get; set; }

        [JsonProperty("cleanupCacheHistory")]
        public bool CleanupCacheHistory { get; set; }

        [JsonProperty("sitemap")]
        public SitemapOptions SitemapOptions { get; set; }

        [JsonProperty("falName")]
        public string FALName { get; set; }

        [JsonProperty("disableGitFeatures")]
        public bool DisableGitFeatures { get; set; }

        [JsonProperty("schemaLicense")]
        public string SchemaLicense { get; set; }
    }
}
