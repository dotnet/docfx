// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx.MarkdigEngine.Extensions;

internal partial class MarkdigExtensionSettingConverter
{
    // Shared JsonSerializerOptions instance.
    internal static readonly System.Text.Json.JsonSerializerOptions DefaultSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                     },
        WriteIndented = false,
    };
}

