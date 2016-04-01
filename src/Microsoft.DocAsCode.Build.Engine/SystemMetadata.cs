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

        [JsonProperty("_path")]
        public string PathFromRoot { get; set; }

        /// <summary>
        /// ROOT TOC file's relative path to ROOT
        /// </summary>
        [JsonProperty("_navRel")]
        public string RootTocRelativePath { get; set; }

        /// <summary>
        /// current file's TOC file's relative path to ROOT
        /// </summary>
        [JsonProperty("_tocRel")]
        public string TocRelativePath { get; set; }
    }
}
