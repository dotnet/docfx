// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class Section
    {
        /// <summary>
        /// The raw content matching the regular expression, e.g. @ABC
        /// </summary>
        [YamlMember(Alias = "key")]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Defines the Markdown Content Location Range
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public List<Location> Locations { get; set; }
    }
}
