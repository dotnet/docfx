// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace Docfx.YamlSerialization.ObjectGraphVisitors;

/// <summary>
/// YamlDotNet behavior has changed since 6.x so a custom version which doesn't check on EnterMapping(IObjectDescriptor).
/// </summary>
internal sealed class ExclusiveObjectGraphVisitor : ChainedObjectGraphVisitor
{
    public ExclusiveObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
        : base(nextVisitor)
    {
    }

    private static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
    {
        var defaultValueAttribute = key.GetCustomAttribute<DefaultValueAttribute>();
        object? defaultValue = defaultValueAttribute != null
            ? defaultValueAttribute.Value
            : GetDefault(key.Type);

        return !Equals(value.Value, defaultValue) && base.EnterMapping(key, value, context);
    }
}
