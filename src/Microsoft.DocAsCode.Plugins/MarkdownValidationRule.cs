// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

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
