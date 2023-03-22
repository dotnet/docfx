// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

[JsonConverter(typeof(MergeJsonConfigConverter))]
internal class MergeJsonConfig : List<MergeJsonItemConfig>
{
    public MergeJsonConfig(IEnumerable<MergeJsonItemConfig> configs) : base(configs) { }

    public MergeJsonConfig(params MergeJsonItemConfig[] configs) : base(configs)
    {
    }
}
