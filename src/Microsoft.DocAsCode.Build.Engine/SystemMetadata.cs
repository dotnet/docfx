// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Newtonsoft.Json;

    internal sealed class SystemMetadata
    {
        [JsonProperty("_lang")]
        public string Language { get; set; }
        [JsonProperty("_title")]
        public string Title { get; set; }
        [JsonProperty("_tocTitle")]
        public string TocTitle { get; set; }
        [JsonProperty("_name")]
        public string Name { get; set; }
        [JsonProperty("_description")]
        public string Description { get; set; }

        /// <summary>
        /// TOC PATH from ~ ROOT
        /// </summary>
        [JsonProperty("_tocPath")]
        public string TocPath { get; set; }

        /// <summary>
        /// ROOT TOC PATH from ~ ROOT
        /// </summary>
        [JsonProperty("_navPath")]
        public string RootTocPath { get; set; }

        /// <summary>
        /// Current file's relative path to ROOT, e.g. file is ~/A/B.md, relative path to ROOT is ../
        /// </summary>
        [JsonProperty("_rel")]
        public string RelativePathToRoot { get; set; }

        /// <summary>
        /// Current file's path from ~ ROOT
        /// </summary>
        [JsonProperty("_path")]
        public string Path { get; set; }

        /// <summary>
        /// Current file's key from ~ ROOT
        /// </summary>
        [JsonProperty("_key")]
        public string Key { get; set; }

        /// <summary>
        /// Current file's relative path to ROOT TOC file
        /// </summary>
        [JsonProperty("_navRel")]
        public string RelativePathToRootToc { get; set; }

        /// <summary>
        /// Current file's relative path to current file's TOC file
        /// </summary>
        [JsonProperty("_tocRel")]
        public string RelativePathToToc { get; set; }

        /// <summary>
        /// The file key for Root TOC file, starting with `~`
        /// </summary>
        [JsonProperty("_navKey")]
        public string RootTocKey { get; set; }

        /// <summary>
        /// The file key for current file's TOC file, starting with `~`
        /// </summary>
        [JsonProperty("_tocKey")]
        public string TocKey { get; set; }

        /// <summary>
        /// Current file's version name
        /// </summary>
        [JsonProperty("_version")]
        public string VersionName { get; set; }

        /// <summary>
        /// Current file's version root path from ~ ROOT
        /// </summary>
        [JsonProperty("_versionPath")]
        public string VersionFolder { get; set; }
    }
}
