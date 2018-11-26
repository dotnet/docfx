// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.ComponentModel;

    using Newtonsoft.Json;

    public class MarkdownMetadataValidationRule
    {
        /// <summary>
        /// The contract name of rule.
        /// </summary>
        [JsonProperty("contractName", Required = Required.Always)]
        public string ContractName { get; set; }

        /// <summary>
        /// Whether to disable this rule by default.
        /// </summary>
        [DefaultValue(false)]
        [JsonProperty("disable")]
        public bool Disable { get; set; }

        public static explicit operator MarkdownMetadataValidationRule(string contractName)
        {
            return new MarkdownMetadataValidationRule { ContractName = contractName };
        }
    }
}
