// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

#nullable enable

namespace Docfx.MarkdigEngine.Extensions;

/// <summary>
/// Markdig extension setting.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
[Newtonsoft.Json.JsonConverter(typeof(MarkdigExtensionSettingConverter.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(MarkdigExtensionSettingConverter.SystemTextJsonConverter))]
public class MarkdigExtensionSetting
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdigExtensionSetting"/> class.
    /// </summary>
    public MarkdigExtensionSetting(string name, JsonNode? options = null)
    {
        Name = name;
        if (options != null)
        {
            Options = options.Deserialize<JsonElement>();
        }
        else
        {
            Options = null;
        }
    }

    /// <summary>
    /// Name of markdig extension
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Options of markdig extension.
    /// This option is storead as immutable JsonElement object.
    /// </summary>
    public JsonElement? Options { get; init; }

    /// <summary>
    /// Gets markdig extension options as specified class object.
    /// </summary>
    public T GetOptions<T>(T fallbackValue)
    {
        return Options is null ? fallbackValue
            : Options.Value.Deserialize<T>(MarkdigExtensionSettingConverter.DefaultSerializerOptions) ?? fallbackValue;
    }

    /// <summary>
    /// Allow implicit cast from markdig extension name.
    /// </summary>
    public static implicit operator MarkdigExtensionSetting(string name)
    {
        return new MarkdigExtensionSetting(name);
    }
}
