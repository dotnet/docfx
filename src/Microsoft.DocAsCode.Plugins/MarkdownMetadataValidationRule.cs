// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

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
