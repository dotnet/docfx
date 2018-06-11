// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyDependencyMapItem
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("type")]
        public LegacyDependencyMapType Type { get; set; }
    }

    internal enum LegacyDependencyMapType
    {
        None,
        Uid,
        Include,
        File,
        Overwrite,
        OverwriteFragments,
        Bookmark,
        Metadata,
    }
}
