// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

[JsonConverter(typeof(BasePathJsonConverter))]
internal readonly struct BasePath
{
    private readonly string? _value;

    /// <summary>
    /// It is either an empty string, or a path without leading or trailing /
    /// </summary>
    public string Value => _value ?? "";

    /// <summary>
    /// Gets or a path starting with `/` for output.
    /// </summary>
    public string ValueWithLeadingSlash => $"/{_value}";

    public BasePath(string value)
    {
        _value = value.Replace('\\', '/').TrimStart('/');
    }

    public override string ToString() => Value;

    public static implicit operator string(BasePath value) => value.Value;

    private class BasePathJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(PathString);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<string>(reader);
            return value is null ? default : new BasePath(value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            => throw new NotSupportedException();
    }
}
