// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

[JsonConverter(typeof(ListWithStringFallbackConverter))]
[Serializable]
internal class ListWithStringFallback : List<string>
{
    public ListWithStringFallback() : base()
    {
    }

    public ListWithStringFallback(IEnumerable<string> list) : base(list)
    {
    }
}
