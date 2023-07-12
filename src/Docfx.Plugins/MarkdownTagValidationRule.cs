// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

using Newtonsoft.Json;

namespace Docfx.Plugins;

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
    /// The contract name for custom validator <see cref="Docfx.Plugins.ICustomMarkdownTagValidator"/>.
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
