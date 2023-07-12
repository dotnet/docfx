// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

using Newtonsoft.Json;

namespace Docfx.Plugins;

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
