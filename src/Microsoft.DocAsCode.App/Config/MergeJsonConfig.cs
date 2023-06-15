// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
