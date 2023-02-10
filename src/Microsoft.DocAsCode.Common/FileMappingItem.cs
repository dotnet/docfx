// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// Data model for a file-mapping item
    /// </summary>
    [Serializable]
    public class FileMappingItem
    {
        private string _sourceFolder;
        private string _cwd;
        private string _version;
        private string _group;

        /// <summary>
        /// The name of current item, the value is not used for now
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The file glob pattern collection, with path relative to property `src`/`cwd` is value is set
        /// </summary>
        [JsonProperty("files")]
        public FileItems Files { get; set; }

        /// <summary>
        /// The file glob pattern collection for files that should be excluded, with path relative to property `src`/`cwd` is value is set
        /// </summary>
        [JsonProperty("exclude")]
        public FileItems Exclude { get; set; }

        /// <summary>
        /// `src` defines the root folder for the source files, it has the same meaning as `cwd`
        /// </summary>
        [JsonProperty("src")]
        public string SourceFolder
        {
            get
            {
                return _sourceFolder;
            }
            set
            {
                _sourceFolder = value;
            }
        }

        /// <summary>
        /// `cwd` defines the root folder for the source files, it has the same meaning as `src`
        /// As discussed, `cwd` may lead to confusing and misunderstanding, so in version 1.3, `src` is introduced and `cwd` is kept for backward compatibility
        /// </summary>
        [JsonProperty("cwd")]
        [Obsolete]
        public string CurrentWorkingDirectory
        {
            get
            {
                return _cwd;
            }
            set
            {
                _cwd = value;
                _sourceFolder = value;
            }
        }

        /// <summary>
        /// The destination folder for the files if copy/transform is used
        /// </summary>
        [JsonProperty("dest")]
        public string DestinationFolder { get; set; }

        [JsonProperty("version")]
        public string VersionName
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        /// <summary>
        /// Group name for the current file-mapping item.
        /// If not set, treat the current file-mapping item as in default group.
        /// Mappings with the same group name will be built together.
        /// Cross reference doesn't support cross different groups.
        /// </summary>
        [JsonProperty("group")]
        public string GroupName
        {
            get
            {
                return _group ?? _version;
            }
            set
            {
                _group = value;
                _version = value;
            }
        }

        /// <summary>
        /// The Root TOC Path used for navbar in current group, relative to output root.
        /// If not set, will use the toc in output root in current group if exists.
        /// </summary>
        [JsonProperty("rootTocPath")]
        public string RootTocPath { get; set; }

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
