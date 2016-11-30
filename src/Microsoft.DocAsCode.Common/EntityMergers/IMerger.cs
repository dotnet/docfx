// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.EntityMergers
{
    using System;

    public interface IMerger
    {
        void Merge(ref object source, object overrides, Type type, IMergeContext context);
        bool TestKey(object source, object overrides, Type type, IMergeContext context);
    }
}
