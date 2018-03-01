// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.NodeDeserializers
{
    using System;

    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.Utilities;

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
                var propertyValuePromise = propertyValue as IValuePromise;
                if (propertyValuePromise == null)
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
}
