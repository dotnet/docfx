// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.TypeInspectors;

public sealed class ExtensibleReadableAndWritablePropertiesTypeInspector : ExtensibleTypeInspectorSkeleton
{
    private readonly IExtensibleTypeInspector _innerTypeDescriptor;

    public ExtensibleReadableAndWritablePropertiesTypeInspector(IExtensibleTypeInspector innerTypeDescriptor)
    {
        _innerTypeDescriptor = innerTypeDescriptor;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
        from p in _innerTypeDescriptor.GetProperties(type, container)
        where p.CanWrite
        select p;

    public override IPropertyDescriptor? GetProperty(Type type, object? container, string name) =>
        _innerTypeDescriptor.GetProperty(type, container, name);
}
