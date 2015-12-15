// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonConverter(typeof(ListWithStringFallbackConverter))]
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
