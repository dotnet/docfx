// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;

    public class FileMappingItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("files")]
        public FileItems Files { get; set; }

        [JsonProperty("exclude")]
        public FileItems Exclude { get; set; }

        [JsonProperty("cwd")]
        public string CurrentWorkingDirectory { get; set; }

        /// <summary>
        /// Pattern match will be case sensitive.
        /// By default the pattern is case insensitive
        /// </summary>
        [JsonProperty("case")]
        public bool? CaseSensitive { get; set; }

        /// <summary>
        /// Disable pattern begin with `!` to mean negate
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noNegate")]
        public bool? DisableNegate { get; set; }

        /// <summary>
        /// Disable `{a,b}c` => `["ac", "bc"]`.
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noExpand")]
        public bool? DisableExpand { get; set; }

        /// <summary>
        /// Disable the usage of `\` to escape values.
        /// By default the usage is enabled.
        /// </summary>
        [JsonProperty("noEscape")]
        public bool? DisableEscape { get; set; }

        /// <summary>
        /// Disable the usage of `**` to match everything including `/` when it is the beginning of the pattern or is after `/`.
        /// By default the usage is enable.
        /// </summary>
        [JsonProperty("noGlobStar")]
        public bool? DisableGlobStar { get; set; }

        /// <summary>
        /// Allow files start with `.` to be matched even if `.` is not explicitly specified in the pattern.
        /// By default files start with `.` will not be matched by `*` unless the pattern starts with `.`.
        /// </summary>
        [JsonProperty("dot")]
        public bool? AllowDotMatch { get; set; }

        public FileMappingItem() { }

        public FileMappingItem(params string[] files)
        {
            Files = new FileItems(files);
        }
    }
}
