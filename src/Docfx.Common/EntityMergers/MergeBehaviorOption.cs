// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.EntityMergers;

public enum MergeOption
{
    /// <summary>
    /// Identify merge item in list.
    /// </summary>
    MergeKey = -2,
    /// <summary>
    /// Do not merge this property.
    /// </summary>
    Ignore = -1,
    /// <summary>
    /// Standard merge(default behavior):
    ///   <list>
    ///     <item>for list, merge the items in the list by the merge key.</item>
    ///     <item>for string or any value type, replace it when it is not null or default value in override entity.</item>
    ///     <item>for other type, merge each property.</item>
    ///   </list>
    /// </summary>
    Merge = 0,
    /// <summary>
    /// When it is not null or default value in override entity, it is same with merge.
    /// When it is null or default value in override entity, it will replace the property to null or default value.
    /// </summary>
    MergeNullOrDefault,
    /// <summary>
    /// Replace it when it is not null or default value in override entity.
    /// </summary>
    Replace,
    /// <summary>
    /// Always replace.
    /// </summary>
    ReplaceNullOrDefault,
}
