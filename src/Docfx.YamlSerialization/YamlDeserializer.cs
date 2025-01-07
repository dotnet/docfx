// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.YamlSerialization.Helpers;
using Docfx.YamlSerialization.NodeDeserializers;
using Docfx.YamlSerialization.NodeTypeResolvers;
using Docfx.YamlSerialization.ObjectFactories;
using Docfx.YamlSerialization.TypeInspectors;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.NodeTypeResolvers;
using YamlDotNet.Serialization.TypeResolvers;
using YamlDotNet.Serialization.Utilities;
using YamlDotNet.Serialization.ValueDeserializers;

namespace Docfx.YamlSerialization;

/// <summary>
/// A facade for the YAML library with the standard configuration.
/// </summary>
public sealed class YamlDeserializer
{
    private static Dictionary<TagName, Type> PredefinedTagMappings { get; } = new()
    {
        { "tag:yaml.org,2002:map", typeof(Dictionary<object, object>) },
        { "tag:yaml.org,2002:bool", typeof(bool) },
        { "tag:yaml.org,2002:float", typeof(double) },
        { "tag:yaml.org,2002:int", typeof(int) },
        { "tag:yaml.org,2002:str", typeof(string) },
        { "tag:yaml.org,2002:timestamp", typeof(DateTime) },
    };

    private readonly Dictionary<TagName, Type> _tagMappings;
    private readonly List<IYamlTypeConverter> _converters;
    private readonly TypeDescriptorProxy _typeDescriptor = new();
    private readonly IValueDeserializer _valueDeserializer;
    private readonly ITypeConverter _reflectionTypeConverter = new ReflectionTypeConverter();

    public IList<INodeDeserializer> NodeDeserializers { get; }
    public IList<INodeTypeResolver> TypeResolvers { get; }
    public IValueDeserializer ValueDeserializer => _valueDeserializer;

    private sealed class TypeDescriptorProxy : ITypeInspector
    {
        public ITypeInspector TypeDescriptor = default!;

        public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
        {
            return TypeDescriptor.GetProperties(type, container);
        }

        public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
        {
            return TypeDescriptor.GetProperty(type, container, name, ignoreUnmatched);
        }
    }

    public YamlDeserializer(
        IObjectFactory? objectFactory = null,
        INamingConvention? namingConvention = null,
        bool ignoreUnmatched = false,
        bool ignoreNotFoundAnchor = true)
    {
        objectFactory ??= new DefaultEmitObjectFactory();
        namingConvention ??= NullNamingConvention.Instance;

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

        NodeDeserializers =
        [
            new TypeConverterNodeDeserializer(_converters),
            new NullNodeDeserializer(),
            new ScalarNodeDeserializer(attemptUnknownTypeDeserialization: false, _reflectionTypeConverter, YamlFormatter.Default, NullNamingConvention.Instance),
            new EmitArrayNodeDeserializer(),
            new EmitGenericDictionaryNodeDeserializer(objectFactory),
            new DictionaryNodeDeserializer(objectFactory, duplicateKeyChecking: true),
            new EmitGenericCollectionNodeDeserializer(objectFactory),
            new CollectionNodeDeserializer(objectFactory, NullNamingConvention.Instance),
            new EnumerableNodeDeserializer(),
            new ExtensibleObjectNodeDeserializer(objectFactory, _typeDescriptor, ignoreUnmatched)
        ];
        _tagMappings = new Dictionary<TagName, Type>(PredefinedTagMappings);
        TypeResolvers =
        [
            new TagNodeTypeResolver(_tagMappings),
            new DefaultContainersNodeTypeResolver(),
            new ScalarYamlNodeTypeResolver()
        ];

        NodeValueDeserializer nodeValueDeserializer = new(NodeDeserializers, TypeResolvers, _reflectionTypeConverter, NullNamingConvention.Instance);
        if (ignoreNotFoundAnchor)
        {
            _valueDeserializer = new LooseAliasValueDeserializer(nodeValueDeserializer);
        }
        else
        {
            _valueDeserializer = new AliasValueDeserializer(nodeValueDeserializer);
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

    public T? Deserialize<T>(TextReader input, IValueDeserializer? deserializer = null)
    {
        return (T?)Deserialize(input, typeof(T), deserializer);
    }

    public object? Deserialize(TextReader input, IValueDeserializer? deserializer = null)
    {
        return Deserialize(input, typeof(object), deserializer);
    }

    public object? Deserialize(TextReader input, Type type, IValueDeserializer? deserializer = null)
    {
        return Deserialize(new Parser(input), type, deserializer);
    }

    public T? Deserialize<T>(IParser reader, IValueDeserializer? deserializer = null)
    {
        return (T?)Deserialize(reader, typeof(T), deserializer);
    }

    public object? Deserialize(IParser reader, IValueDeserializer? deserializer = null)
    {
        return Deserialize(reader, typeof(object), deserializer);
    }

    /// <summary>
    /// Deserializes an object of the specified type.
    /// </summary>
    /// <param name="parser">The <see cref="IParser" /> where to deserialize the object.</param>
    /// <param name="type">The static type of the object to deserialize.</param>
    /// <returns>Returns the deserialized object.</returns>
    public object? Deserialize(IParser parser, Type type, IValueDeserializer? deserializer = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(type);

        var hasStreamStart = parser.TryConsume<StreamStart>(out _);

        var hasDocumentStart = parser.TryConsume<DocumentStart>(out _);
        deserializer ??= _valueDeserializer;
        object? result = null;
        if (!parser.Accept<DocumentEnd>(out _) && !parser.Accept<StreamEnd>(out _))
        {
            using var state = new SerializerState();
            result = deserializer.DeserializeValue(parser, type, state, deserializer);
            state.OnDeserialization();
        }

        if (hasDocumentStart)
        {
            parser.Consume<DocumentEnd>();
        }

        if (hasStreamStart)
        {
            parser.Consume<StreamEnd>();
        }

        return result;
    }

    private sealed class LooseAliasValueDeserializer : IValueDeserializer
    {
        private readonly IValueDeserializer _innerDeserializer;

        public LooseAliasValueDeserializer(IValueDeserializer innerDeserializer)
        {
            ArgumentNullException.ThrowIfNull(innerDeserializer);

            _innerDeserializer = innerDeserializer;
        }

        private sealed class AliasState : Dictionary<AnchorName, ValuePromise>, IPostDeserializationCallback
        {
            public void OnDeserialization()
            {
                foreach (var promise in Values)
                {
                    if (!promise.HasValue && promise.Alias != null)
                    {
                        // If promise is not resolved, reset to it's alias value
                        promise.Value = "*" + promise.Alias.Value;
                    }
                }
            }
        }

        private sealed class ValuePromise : IValuePromise
        {
            public event Action<object?>? ValueAvailable;

            public bool HasValue { get; private set; }

            private object? value;

            public readonly AnchorAlias? Alias;

            public ValuePromise(AnchorAlias alias)
            {
                Alias = alias;
            }

            public ValuePromise(object? value)
            {
                HasValue = true;
                this.value = value;
            }

            public object? Value
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

        public object? DeserializeValue(IParser reader, Type expectedType, SerializerState state, IValueDeserializer nestedObjectDeserializer)
        {
            object? value;
            if (reader.TryConsume<AnchorAlias>(out var alias))
            {
                var aliasState = state.Get<AliasState>();
                if (!aliasState.TryGetValue(alias.Value, out var valuePromise))
                {
                    valuePromise = new ValuePromise(alias);
                    aliasState.Add(alias.Value, valuePromise);
                }

                return valuePromise.HasValue ? valuePromise.Value : valuePromise;
            }

            AnchorName? anchor = null;

            if (reader.Accept<NodeEvent>(out var nodeEvent) && !nodeEvent.Anchor.IsEmpty)
            {
                anchor = nodeEvent.Anchor;
            }

            value = _innerDeserializer.DeserializeValue(reader, expectedType, state, nestedObjectDeserializer);

            if (anchor != null)
            {
                var aliasState = state.Get<AliasState>();

                if (!aliasState.TryGetValue(anchor.Value, out var valuePromise))
                {
                    aliasState.Add(anchor.Value, new ValuePromise(value));
                }
                else if (!valuePromise.HasValue)
                {
                    valuePromise.Value = value;
                }
                else
                {
                    aliasState[anchor.Value] = new ValuePromise(value);
                }
            }

            return value;
        }
    }
}
