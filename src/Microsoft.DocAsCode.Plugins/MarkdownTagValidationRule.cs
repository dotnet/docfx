// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.ComponentModel;

    using Newtonsoft.Json;

    public class MarkdownTagValidationRule
    {
        /// <summary>
        /// The names of tag.
        /// </summary>
        [JsonProperty("tagNames", Required = Required.Always)]
        public List<string> TagNames { get; set; }
        /// <summary>
        /// The relation for tags.
        /// </summary>
        [JsonProperty("relation")]
        public TagRelation Relation { get; set; }
        /// <summary>
        /// Define tag's behavior.
        /// </summary>
        [JsonProperty("behavior", Required = Required.Always)]
        public TagValidationBehavior Behavior { get; set; }
        /// <summary>
        /// The message formatter for warning and error. '{0}' is name of tag, '{1}' is the full tag.
        /// </summary>
        [JsonProperty("messageFormatter", Required = Required.Always)]
        public string MessageFormatter { get; set; }
        /// <summary>
        /// The contract name for custom validator <see cref="Microsoft.DocAsCode.Plugins.ICustomMarkdownTagValidator"/>.
        /// </summary>
        [JsonProperty("customValidatorContractName")]
        public string CustomValidatorContractName { get; set; }
        /// <summary>
        /// Only validate opening tag.
        /// </summary>
        [JsonProperty("openingTagOnly")]
        public bool OpeningTagOnly { get; set; }
        /// <summary>
        /// Whether to disable this rule by default.
        /// </summary>
        [DefaultValue(false)]
        [JsonProperty("disable")]
        public bool Disable { get; set; }
    }
}
