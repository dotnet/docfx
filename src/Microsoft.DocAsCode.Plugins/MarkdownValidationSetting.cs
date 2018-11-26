// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.ComponentModel;

    using Newtonsoft.Json;

    public class MarkdownValidationSetting
    {
        /// <summary>
        /// The category of rule
        /// </summary>
        [JsonProperty("category", Required = Required.Always)]
        public string Category { get; set; }
        /// <summary>
        /// The id of rule.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// Whether to disable this rule by default.
        /// </summary>
        [DefaultValue(false)]
        [JsonProperty("disable")]
        public bool Disable { get; set; }

        public static explicit operator MarkdownValidationSetting(string category)
        {
            return new MarkdownValidationSetting { Category = category };
        }
    }
}
