// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Docfx.Plugins;

#nullable enable

namespace Docfx.Common;

/// <summary>
/// Utility class for JSON serialization/deserialization.
/// </summary>
internal static class SystemTextJsonUtility
{
    /// <summary>
    /// Default JsonSerializerOptions options.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultSerializerOptions;

    /// <summary>
    ///  Default JsonSerializerOptions options with indent setting.
    /// </summary>
    public static readonly JsonSerializerOptions IndentedSerializerOptions;

    static SystemTextJsonUtility()
    {
        DefaultSerializerOptions = new JsonSerializerOptions()
        {
            // DefaultBufferSize = 1024 * 16, // TODO: Set appropriate buffer size based on benchmark.(Default: 16KB)
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // TODO: Replace with custom encoder that encode minimal chars (https://github.com/dotnet/runtime/issues/87153)
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // DictionaryKeyPolicy = JsonNamingPolicy.CamelCase, // This setting is not compatible to `Newtonsoft.Json` serialize result.
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ObjectToInferredTypesConverter(), // Required for `Dictionary<string, object>` type deserialization.
            },
            WriteIndented = false,
        };

        IndentedSerializerOptions = new JsonSerializerOptions(DefaultSerializerOptions)
        {
            WriteIndented = true,
        };
    }

    /// <summary>
    /// Serialize model to JSON string.
    /// </summary>
    public static string Serialize<T>(T model, bool indented = false)
    {
        var options = indented
            ? IndentedSerializerOptions
            : DefaultSerializerOptions;

        return JsonSerializer.Serialize(model, options);
    }

    /// <summary>
    /// Serialize stream to JSON string.
    /// </summary>
    public static string Serialize<T>(Stream stream, bool indented = false)
    {
        var options = indented
            ? IndentedSerializerOptions
            : DefaultSerializerOptions;

        return JsonSerializer.Serialize(stream, options);
    }

    /// <summary>
    /// Deserialize model from JSON string.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultSerializerOptions);
    }

    /// <summary>
    /// Deserialize model from stream.
    /// </summary>
    public static T? Deserialize<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, DefaultSerializerOptions);
    }

    /// <summary>
    /// Deserialize model from stream.
    /// </summary>
    public static async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken token = default)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, DefaultSerializerOptions, cancellationToken: token);
    }

    /// <summary>
    /// Serialize specified model to file.
    /// </summary>
    public static void SerializeToFile<T>(string path, T model, bool indented = false)
    {
        var options = indented
            ? IndentedSerializerOptions
            : DefaultSerializerOptions;
        using var stream = EnvironmentContext.FileAbstractLayer.Create(path);
        JsonSerializer.Serialize(stream, model, options);
    }

    /// <summary>
    /// Deserialize specified model from JSON file.
    /// </summary>
    public static T? DeserializeFromFile<T>(string path)
    {
        using var stream = EnvironmentContext.FileAbstractLayer.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, DefaultSerializerOptions);
    }
}
