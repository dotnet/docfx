// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations.Helpers
{
    using System.Collections.Generic;

    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.Converters;

    internal static class YamlTypeConverters
    {
        private static readonly IEnumerable<IYamlTypeConverter> _builtInTypeConverters =
            new IYamlTypeConverter[]
            {
                new GuidConverter(),
            };

        public static IEnumerable<IYamlTypeConverter> BuiltInConverters => _builtInTypeConverters;
    }
}
