// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public sealed class XRefSpecUidComparer : Comparer<XRefSpec>
{
    public static readonly XRefSpecUidComparer Instance = new();

    public override int Compare(XRefSpec x, XRefSpec y)
    {
        return StringComparer.InvariantCulture.Compare(x.Uid, y.Uid);
    }
}
