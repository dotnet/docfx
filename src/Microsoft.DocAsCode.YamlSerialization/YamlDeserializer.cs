// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    using YamlDotNet.Serialization.NodeDeserializers;
    using YamlDotNet.Serialization.NodeTypeResolvers;
    using YamlDotNet.Serialization.TypeResolvers;
    using YamlDotNet.Serialization.Utilities;
    using YamlDotNet.Serialization.ValueDeserializers;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;
    using Microsoft.DocAsCode.YamlSerialization.NodeDeserializers;
    using Microsoft.DocAsCode.YamlSerialization.NodeTypeResolvers;
    using Microsoft.DocAsCode.YamlSerialization.ObjectFactories;
    using Microsoft.DocAsCode.YamlSerialization.TypeInspectors;

    /// <summary>
    /// A façade for the YAML library with the standard configuration.
    /// </summary>
    public sealed class YamlDeserializer
    {
        private static Dictionary<string, Type> PredefinedTagMappings { get; } =
            new Dictionary<string, Type>
            {
                { "tag:yaml.org,2002:map", typeof(Dictionary<object, object>) },
                { "tag:yaml.org,2002:bool", typeof(bool) },
                { "tag:yaml.org,2002:float", typeof(double) },
                { "tag:yaml.org,2002:int", typeof(int) },
                { "tag:yaml.org,2002:str", typeof(string) },
                { "tag:yaml.org,2002:timestamp", typeof(DateTime) },
            };

        private readonly Dictionary<string, Type> _tagMappings;
        private readonly List<IYamlTypeConverter> _converters;
        private readonly TypeDescriptorProxy _typeDescriptor =
            new TypeDescriptorProxy();
        private readonly IValueDeserializer _valueDeserializer;

        public IList<INodeDeserializer> NodeDeserializers { get; private set; }
        public IList<INodeTypeResolver> TypeResolvers { get; private set; }
        public IValueDeserializer ValueDeserializer => _valueDeserializer;
        private class TypeDescriptorProxy : ITypeInspector
        {
            public ITypeInspector TypeDescriptor;

            public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
            {
                return TypeDescriptor.GetProperties(type, container);
            }

            public IPropertyDescriptor GetProperty(Type type, object container, string name, bool ignoreUnmatched)
            {
                return TypeDescriptor.GetProperty(type, container, name, ignoreUnmatched);
            }
        }

        public YamlDeserializer(
            IObjectFactory objectFactory = null,
            INamingConvention namingConvention = null,
            bool ignoreUnmatched = false,
            bool ignoreNotFoundAnchor = true)
        {
            objectFactory = objectFactory ?? new DefaultEmitObjectFactory();
            namingConvention = namingConvention ?? new NullNamingConvention();

            _typeDescriptor.TypeDescriptor =
                new ExtensibleYamlAttributesTypeInspector(
                    new ExtensibleNamingConventionTypeInspector(
                        new ExtensibleReadableAndWritablePropertiesTypeInspector(
                            new EmitTypeInspector(
                                new StaticTypeResolver()
                            )
                        ),
                        namingConvention
                    )
                );

            _converters = new List<IYamlTypeConverter>();
            foreach (IYamlTypeConverter yamlTypeConverter in YamlTypeConverters.BuiltInConverters)
            {
                _converters.Add(yamlTypeConverter);
            }

            NodeDeserializers = new List<INodeDeserializer>
            {
                new TypeConverterNodeDeserializer(_converters),
                new NullNodeDeserializer(),
                new ScalarNodeDeserializer(),
                new EmitArrayNodeDeserializer(),
                new EmitGenericDictionaryNodeDeserializer(objectFactory),
                new DictionaryNodeDeserializer(objectFactory),
                new EmitGenericCollectionNodeDeserializer(objectFactory),
                new CollectionNodeDeserializer(objectFactory),
                new EnumerableNodeDeserializer(),
                new ExtensibleObjectNodeDeserializer(objectFactory, _typeDescriptor, ignoreUnmatched)
            };
            _tagMappings = new Dictionary<string, Type>(PredefinedTagMappings);
            TypeResolvers = new List<INodeTypeResolver>
            {
                new TagNodeTypeResolver(_tagMappings),
                new DefaultContainersNodeTypeResolver(),
                new ScalarYamlNodeTypeResolver()
            };
            if (ignoreNotFoundAnchor)
            {
                _valueDeserializer =
                    new LooseAliasValueDeserializer(
                        new NodeValueDeserializer(
                            NodeDeserializers,
                            TypeResolvers
                        )
                    );
            }
            else
            {
                _valueDeserializer =
                    new AliasValueDeserializer(
                        new NodeValueDeserializer(
                            NodeDeserializers,
                            TypeResolvers
                        )
                    );
            }
        }

        public void RegisterTagMapping(string tag, Type type)
        {
            _tagMappings.Add(tag, type);
        }

        public void RegisterTypeConverter(IYamlTypeConverter typeConverter)
        {
            _converters.Add(typeConverter);
        }

        public T Deserialize<T>(TextReader input, IValueDeserializer deserializer = null)
        {
            return (T)Deserialize(input, typeof(T), deserializer);
        }

        public object Deserialize(TextReader input, IValueDeserializer deserializer = null)
        {
            return Deserialize(input, typeof(object), deserializer);
        }

        public object Deserialize(TextReader input, Type type, IValueDeserializer deserializer = null)
        {
            return Deserialize(new Parser(input), type, deserializer);
        }

        public T Deserialize<T>(IParser reader, IValueDeserializer deserializer = null)
        {
            return (T)Deserialize(reader, typeof(T), deserializer);
        }

        public object Deserialize(IParser reader, IValueDeserializer deserializer = null)
        {
            return Deserialize(reader, typeof(object), deserializer);
        }

        /// <summary>
        /// Deserializes an object of the specified type.
        /// </summary>
        /// <param name="parser">The <see cref="IParser" /> where to deserialize the object.</param>
        /// <param name="type">The static type of the object to deserialize.</param>
        /// <returns>Returns the deserialized object.</returns>
        public object Deserialize(IParser parser, Type type, IValueDeserializer deserializer = null)
        {
            if (parser == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var hasStreamStart = parser.Allow<StreamStart>() != null;

            var hasDocumentStart = parser.Allow<DocumentStart>() != null;
            deserializer = deserializer ?? _valueDeserializer;
            object result = null;
            if (!parser.Accept<DocumentEnd>() && !parser.Accept<StreamEnd>())
            {
                using (var state = new SerializerState())
                {
                    result = deserializer.DeserializeValue(parser, type, state, deserializer);
                    state.OnDeserialization();
                }
            }

            if (hasDocumentStart)
            {
                parser.Expect<DocumentEnd>();
            }

            if (hasStreamStart)
            {
                parser.Expect<StreamEnd>();
            }

            return result;
        }

        private sealed class LooseAliasValueDeserializer : IValueDeserializer
        {
            private readonly IValueDeserializer _innerDeserializer;

            public LooseAliasValueDeserializer(IValueDeserializer innerDeserializer)
            {
                _innerDeserializer = innerDeserializer ?? throw new ArgumentNullException("innerDeserializer");
            }

            private sealed class AliasState : Dictionary<string, ValuePromise>, IPostDeserializationCallback
            {
                public void OnDeserialization()
                {
                    foreach (var promise in Values)
                    {
                        if (!promise.HasValue)
                        {
                            // If promise is not resolved, reset to it's alias value
                            promise.Value = "*" + promise.Alias.Value;
                        }
                    }
                }
            }

            private sealed class ValuePromise : IValuePromise
            {
                public event Action<object> ValueAvailable;

                public bool HasValue { get; private set; }

                private object value;

                public readonly AnchorAlias Alias;

                public ValuePromise(AnchorAlias alias)
                {
                    this.Alias = alias;
                }

                public ValuePromise(object value)
                {
                    HasValue = true;
                    this.value = value;
                }

                public object Value
                {
                    get
                    {
                        if (!HasValue)
                        {
                            throw new InvalidOperationException("Value not set");
                        }
                        return value;
                    }
                    set
                    {
                        if (HasValue)
                        {
                            throw new InvalidOperationException("Value already set");
                        }
                        HasValue = true;
                        this.value = value;

                        ValueAvailable?.Invoke(value);
                    }
                }
            }

            public object DeserializeValue(IParser reader, Type expectedType, SerializerState state, IValueDeserializer nestedObjectDeserializer)
            {
                object value;
                var alias = reader.Allow<AnchorAlias>();
                if (alias != null)
                {
                    var aliasState = state.Get<AliasState>();
                    if (!aliasState.TryGetValue(alias.Value, out ValuePromise valuePromise))
                    {
                        valuePromise = new ValuePromise(alias);
                        aliasState.Add(alias.Value, valuePromise);
                    }

                    return valuePromise.HasValue ? valuePromise.Value : valuePromise;
                }

                string anchor = null;

                var nodeEvent = reader.Peek<NodeEvent>();
                if (nodeEvent != null && !string.IsNullOrEmpty(nodeEvent.Anchor))
                {
                    anchor = nodeEvent.Anchor;
                }

                value = _innerDeserializer.DeserializeValue(reader, expectedType, state, nestedObjectDeserializer);

                if (anchor != null)
                {
                    var aliasState = state.Get<AliasState>();

                    if (!aliasState.TryGetValue(anchor, out ValuePromise valuePromise))
                    {
                        aliasState.Add(anchor, new ValuePromise(value));
                    }
                    else if (!valuePromise.HasValue)
                    {
                        valuePromise.Value = value;
                    }
                    else
                    {
                        aliasState[anchor] = new ValuePromise(value);
                    }
                }

                return value;
            }
        }
    }
}