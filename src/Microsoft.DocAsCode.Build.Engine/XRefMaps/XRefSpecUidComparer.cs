// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    public sealed class XRefSpecUidComparer : Comparer<XRefSpec>
    {
        public static readonly XRefSpecUidComparer Instance = new XRefSpecUidComparer();

        public override int Compare(XRefSpec x, XRefSpec y)
        {
            return StringComparer.InvariantCulture.Compare(x.Uid, y.Uid);
        }
    }
}
