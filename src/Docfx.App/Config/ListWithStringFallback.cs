// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// ListWithStringFallback.
/// </summary>
[JsonConverter(typeof(ListWithStringFallbackConverter))]
[Serializable]
internal class ListWithStringFallback : List<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListWithStringFallback"/> class.
    /// </summary>
    public ListWithStringFallback() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListWithStringFallback"/> class.
    /// </summary>
    /// <param name="list">The collection whose elements are copied to the new list.</param>
    public ListWithStringFallback(IEnumerable<string> list) : base(list)
    {
    }
}
