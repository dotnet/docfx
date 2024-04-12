// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

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
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // DictionaryKeyPolicy = JsonNamingPolicy.CamelCase, // This setting is not compatible to `Newtonsoft.Json` serialize result.
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters =
            {
                new JsonStringEnumConverter(),
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
    /// Converts the value of a type specified by a generic type parameter into a JSON string.
    /// </summary>
    public static string Serialize<T>(T model, bool indented = false)
    {
        var options = indented
            ? IndentedSerializerOptions
            : DefaultSerializerOptions;

        return JsonSerializer.Serialize(model, options);
    }

    /// <summary>
    /// Converts the value of a type specified by a generic type parameter into a JSON string.
    /// </summary>
    public static string Serialize<T>(Stream stream, bool indented = false)
    {
        var options = indented
            ? IndentedSerializerOptions
            : DefaultSerializerOptions;

        return JsonSerializer.Serialize(stream, options);
    }

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a TValue.
    /// The Stream will be read to completion.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultSerializerOptions);
    }

    /// <summary>
    /// Reads the UTF-8 encoded text representing a single JSON value into a TValue.
    /// The Stream will be read to completion.
    /// </summary>
    public static T? Deserialize<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, DefaultSerializerOptions);
    }

    /// <summary>
    /// Asynchronously reads the UTF-8 encoded text representing a single JSON value
    //  into an instance of a type specified by a generic type parameter. The stream
    //  will be read to completion.
    public static async ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken token = default)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, DefaultSerializerOptions, cancellationToken: token);
    }
}
