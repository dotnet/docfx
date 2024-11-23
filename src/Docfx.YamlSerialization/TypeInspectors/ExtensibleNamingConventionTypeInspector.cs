// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.TypeInspectors;

public sealed class ExtensibleNamingConventionTypeInspector : ExtensibleTypeInspectorSkeleton
{
    private readonly IExtensibleTypeInspector innerTypeDescriptor;
    private readonly INamingConvention namingConvention;

    public ExtensibleNamingConventionTypeInspector(IExtensibleTypeInspector innerTypeDescriptor, INamingConvention namingConvention)
    {
        ArgumentNullException.ThrowIfNull(innerTypeDescriptor);
        ArgumentNullException.ThrowIfNull(namingConvention);

        this.innerTypeDescriptor = innerTypeDescriptor;
        this.namingConvention = namingConvention;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
        from p in innerTypeDescriptor.GetProperties(type, container)
        select (IPropertyDescriptor)new PropertyDescriptor(p) { Name = namingConvention.Apply(p.Name) };

    public override IPropertyDescriptor? GetProperty(Type type, object? container, string name) =>
        innerTypeDescriptor.GetProperty(type, container, name);
}
