// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace Microsoft.DocAsCode.YamlSerialization.Helpers;

internal static class YamlTypeConverters
{
    private static readonly IEnumerable<IYamlTypeConverter> _builtInTypeConverters =
        new IYamlTypeConverter[]
        {
            new GuidConverter(false),
        };

    public static IEnumerable<IYamlTypeConverter> BuiltInConverters => _builtInTypeConverters;
}
