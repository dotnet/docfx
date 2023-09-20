// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public sealed class TemplateResourceInfo
{
    public string ResourceKey { get; }
    public TemplateResourceInfo(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public override bool Equals(object obj)
    {
        if (obj is not TemplateResourceInfo target)
        {
            return false;
        }

        if (ReferenceEquals(this, target))
        {
            return true;
        }

        return Equals(ResourceKey, target.ResourceKey);
    }

    public override int GetHashCode()
    {
        return ResourceKey.GetHashCode();
    }
}
