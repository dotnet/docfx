// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
