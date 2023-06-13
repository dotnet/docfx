// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Common.EntityMergers;

public interface IMerger
{
    void Merge(ref object source, object overrides, Type type, IMergeContext context);
    bool TestKey(object source, object overrides, Type type, IMergeContext context);
}
