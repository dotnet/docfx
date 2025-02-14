// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Docfx.YamlSerialization.NodeDeserializers;

public sealed class ExtensibleObjectNodeDeserializer : INodeDeserializer
{
    private readonly IObjectFactory _objectFactory;
    private readonly ITypeInspector _typeDescriptor;
    private readonly bool _ignoreUnmatched;
    private readonly bool _caseInsensitivePropertyMatching;
    private readonly INamingConvention _enumNamingConvention;

    public ExtensibleObjectNodeDeserializer(IObjectFactory objectFactory, ITypeInspector typeDescriptor, INamingConvention enumNamingConvention, bool ignoreUnmatched, bool caseInsensitivePropertyMatching)
    {
        _objectFactory = objectFactory;
        _typeDescriptor = typeDescriptor;
        _enumNamingConvention = enumNamingConvention;
        _ignoreUnmatched = ignoreUnmatched;
        _caseInsensitivePropertyMatching = caseInsensitivePropertyMatching;
    }

    bool INodeDeserializer.Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
    {
        if (!reader.TryConsume<MappingStart>(out _))
        {
            value = null;
            return false;
        }

        value = _objectFactory.Create(expectedType);
        while (!reader.Accept<MappingEnd>(out _))
        {
            var propertyName = reader.Consume<Scalar>();
            var property = _typeDescriptor.GetProperty(expectedType, value, propertyName.Value, _ignoreUnmatched, _caseInsensitivePropertyMatching);
            if (property == null)
            {
                reader.SkipThisAndNestedEvents();
                continue;
            }

            var propertyValue = nestedObjectDeserializer(reader, property.Type);
            if (propertyValue is not IValuePromise propertyValuePromise)
            {
                var convertedValue = TypeConverter.ChangeType(propertyValue, property.Type, _enumNamingConvention, _typeDescriptor);
                property.Write(value, convertedValue);
            }
            else
            {
                var valueRef = value;
                propertyValuePromise.ValueAvailable += v =>
                {
                    var convertedValue = TypeConverter.ChangeType(v, property.Type, _enumNamingConvention, _typeDescriptor);
                    property.Write(valueRef, convertedValue);
                };
            }
        }

        reader.Consume<MappingEnd>();
        return true;
    }
}
