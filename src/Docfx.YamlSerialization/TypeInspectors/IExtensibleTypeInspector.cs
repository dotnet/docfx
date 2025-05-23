﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.TypeInspectors;

public interface IExtensibleTypeInspector : ITypeInspector
{
    IPropertyDescriptor? GetProperty(Type type, object? container, string name);
}
