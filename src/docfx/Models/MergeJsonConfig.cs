// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    [JsonConverter(typeof(MergeJsonConfigConverter))]
    public class MergeJsonConfig : List<MergeJsonItemConfig>
    {
        public MergeJsonConfig(IEnumerable<MergeJsonItemConfig> configs) : base(configs) { }

        public MergeJsonConfig(params MergeJsonItemConfig[] configs) : base(configs)
        {
        }
    }
}
