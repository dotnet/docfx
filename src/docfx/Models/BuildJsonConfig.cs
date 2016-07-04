// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    [Serializable]
    public class BuildJsonConfig : BuildJsonConfigCommon
    {
        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonIgnore]
        public string OutputFolder { get; set; }

        [JsonProperty("template")]
        public ListWithStringFallback Templates { get; set; } = new ListWithStringFallback();

        [JsonProperty("theme")]
        public ListWithStringFallback Themes { get; set; }

        [JsonProperty("serve")]
        public bool? Serve { get; set; }

        [JsonProperty("force")]
        public bool? Force { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("exportRawModel")]
        public bool? ExportRawModel { get; set; }

        [JsonProperty("exportViewModel")]
        public bool? ExportViewModel { get; set; }

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

        [JsonProperty("docsets")]
        public List<BuildJsonConfigCommon> Docsets { get; set; }

    }
}
