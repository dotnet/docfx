// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.EntityMergers;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class MergeOptionAttribute : Attribute
{
    public MergeOptionAttribute(MergeOption option = MergeOption.Merge)
    {
        Option = option;
    }

    /// <summary>
    /// Hint merger use custom merge handler.
    /// </summary>
    /// <param name="handlerType">the type of custom merge handler, it should implement <see cref="IMergeHandler"/>.</param>
    public MergeOptionAttribute(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        Option = MergeOption.Merge;
        Handler = (IMergeHandler)Activator.CreateInstance(handlerType);
    }

    public MergeOption Option { get; }

    public IMergeHandler Handler { get; }
}
