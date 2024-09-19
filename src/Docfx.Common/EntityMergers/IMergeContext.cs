// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.EntityMergers;

public interface IMergeContext
{
    IMerger Merger { get; }
    object this[string key] { get; }
}
