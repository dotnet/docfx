// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

/// <summary>
/// ListWithStringFallback.
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(ListWithStringFallbackConverter.NewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(ListWithStringFallbackConverter.SystemTextJsonConverter))]
internal class ListWithStringFallback : List<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListWithStringFallback"/> class.
    /// </summary>
    public ListWithStringFallback()
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
