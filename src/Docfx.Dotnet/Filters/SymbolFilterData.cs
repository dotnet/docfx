// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class SymbolFilterData
{
    public string Id { get; set; }

    public ExtendedSymbolKind? Kind { get; set; }

    public IEnumerable<AttributeFilterData> Attributes { get; set; }
}
