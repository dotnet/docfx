// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonConverter(typeof(ListWithStringFallbackConverter))]
    [Serializable]
    public class ListWithStringFallback : List<string>
    {
        public ListWithStringFallback() : base()
        {
        }

        public ListWithStringFallback(IEnumerable<string> list) : base(list)
        {
        }
    }
}
