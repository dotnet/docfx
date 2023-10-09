// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public sealed class EmptyResourceReader : ResourceFileReader
{
    private static readonly IEnumerable<string> Empty = Array.Empty<string>();

    public override bool IsEmpty => true;
    public override string Name => "Empty";

    public override IEnumerable<string> Names => Empty;

    public override Stream GetResourceStream(string name)
    {
        return Stream.Null;
    }
}
