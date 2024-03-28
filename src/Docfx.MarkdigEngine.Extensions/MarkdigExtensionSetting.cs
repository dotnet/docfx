// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx.MarkdigEngine.Extensions;

/// <summary>
/// Markdig extension setting.
/// </summary>
[DebuggerDisplay(@"Name = {Name}")]
[Newtonsoft.Json.JsonConverter(typeof(MarkdigExtensionSettingConverter))]
public class MarkdigExtensionSetting
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
                        new JsonStringEnumConverter()
                     },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdigExtensionSetting"/> class.
    /// </summary>
    public MarkdigExtensionSetting(string name, JsonObject? options = null)
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
        if (Options == null)
        {
            return fallbackValue;
        }

        var jsonObject = JsonSerializer.SerializeToNode(Options)?.AsObject();

        if (jsonObject != null
         && jsonObject.TryGetPropertyValue("options", out var optionsNode)
         && optionsNode != null)
        {
            return optionsNode.Deserialize<T>(DefaultSerializerOptions)!;
        }
        else
        {
            return fallbackValue;
        }
    }

    /// <summary>
    /// Gets markdig extension options as specified class object.
    /// </summary>
    public T GetOptionsValue<T>(string key, T fallbackValue)
    {
        if (Options == null)
        {
            return fallbackValue;
        }

        var jsonNode = JsonSerializer.SerializeToNode(Options)?.AsObject();

        // Try to read options property that have specified key.
        if (jsonNode != null
         && jsonNode.TryGetPropertyValue("options", out var optionsNode)
         && optionsNode != null
         && optionsNode.AsObject().TryGetPropertyValue(key, out var valueNode))
        {
            return valueNode!.GetValue<T>()!;
        }
        else
        {
            return fallbackValue;
        }
    }

    /// <summary>
    /// Allow implicit cast from markdig extension name.
    /// </summary>
    public static implicit operator MarkdigExtensionSetting(string name)
    {
        return new MarkdigExtensionSetting(name);
    }
}
