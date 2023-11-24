// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class AttributeFilterData
{
    public string Id { get; set; }

    public IEnumerable<string> ConstructorArguments { get; set; }

    public IDictionary<string, string> ConstructorNamedArguments { get; set; }
}
