// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public sealed class XRefSpecUidComparer : Comparer<XRefSpec>
{
    public static readonly XRefSpecUidComparer Instance = new();

    public override int Compare(XRefSpec x, XRefSpec y)
    {
        return StringComparer.InvariantCulture.Compare(x.Uid, y.Uid);
    }
}
