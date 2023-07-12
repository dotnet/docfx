// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace Docfx.YamlSerialization.Helpers;

internal static class YamlTypeConverters
{
    private static readonly IEnumerable<IYamlTypeConverter> _builtInTypeConverters =
        new IYamlTypeConverter[]
        {
            new GuidConverter(false),
        };

    public static IEnumerable<IYamlTypeConverter> BuiltInConverters => _builtInTypeConverters;
}
