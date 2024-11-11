// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Outline.Destinations;

#nullable enable

namespace Docfx;

internal static class PdfPigTypeExtensions
{
    public static NamedDestinations GetNamedDestinations(this Catalog catalog)
        => GetNamedDestinationsProperty(catalog);

    public static bool TryGet(this NamedDestinations namedDestinations, string name, out ExplicitDestination dest)
        => TryGetNamedDestinations(namedDestinations, name, out dest);

    // Gets property value of catalog.NamedDestination.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_NamedDestinations")]
    private static extern NamedDestinations GetNamedDestinationsProperty(Catalog value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TryGet")]
    private static extern bool TryGetNamedDestinations(NamedDestinations namedDestinations, string name, out ExplicitDestination dest);
}

