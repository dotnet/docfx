// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Microsoft.DocAsCode.YamlSerialization.NodeDeserializers;

public sealed class ExtensibleObjectNodeDeserializer : INodeDeserializer
{
    private readonly IObjectFactory _objectFactory;
    private readonly ITypeInspector _typeDescriptor;
    private readonly bool _ignoreUnmatched;

    public ExtensibleObjectNodeDeserializer(IObjectFactory objectFactory, ITypeInspector typeDescriptor, bool ignoreUnmatched)
    {
        _objectFactory = objectFactory;
        _typeDescriptor = typeDescriptor;
        _ignoreUnmatched = ignoreUnmatched;
    }

    bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
    {
        var mapping = reader.Allow<MappingStart>();
        if (mapping == null)
        {
            value = null;
            return false;
        }

        value = _objectFactory.Create(expectedType);
        while (!reader.Accept<MappingEnd>())
        {
            var propertyName = reader.Expect<Scalar>();
            var property = _typeDescriptor.GetProperty(expectedType, value, propertyName.Value, _ignoreUnmatched);
            if (property == null)
            {
                reader.SkipThisAndNestedEvents();
                continue;
            }

            var propertyValue = nestedObjectDeserializer(reader, property.Type);
            if (!(propertyValue is IValuePromise propertyValuePromise))
            {
                var convertedValue = TypeConverter.ChangeType(propertyValue, property.Type);
                property.Write(value, convertedValue);
            }
            else
            {
                var valueRef = value;
                propertyValuePromise.ValueAvailable += v =>
                {
                    var convertedValue = TypeConverter.ChangeType(v, property.Type);
                    property.Write(valueRef, convertedValue);
                };
            }
        }

        reader.Expect<MappingEnd>();
        return true;
    }
}
