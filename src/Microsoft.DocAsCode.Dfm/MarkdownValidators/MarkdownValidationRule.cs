﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.MarkdownValidators
{
    using System;
    using System.ComponentModel;

    using Newtonsoft.Json;

    public class MarkdownValidationRule
    {
        /// <summary>
        /// The contract name of rule.
        /// </summary>
        [Obsolete("Please use ContractName.")]
        [JsonProperty("name")]
        public string RuleName
        {
            get { return ContractName; }
            set { ContractName = value; }
        }

        /// <summary>
        /// The contract name of rule.
        /// </summary>
        [JsonProperty("contractName")]
        public string ContractName { get; set; }

        /// <summary>
        /// Whether to disable this rule by default.
        /// </summary>
        [DefaultValue(false)]
        [JsonProperty("disable")]
        public bool Disable { get; set; }

        public static explicit operator MarkdownValidationRule(string contractName)
        {
            return new MarkdownValidationRule { ContractName = contractName };
        }
    }
}
