// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.YamlSerialization;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExtensibleMemberAttribute : Attribute
{
    public string Prefix { get; }

    public ExtensibleMemberAttribute()
        : this(string.Empty)
    {
    }

    public ExtensibleMemberAttribute(string prefix)
    {
        Prefix = prefix;
    }
}
