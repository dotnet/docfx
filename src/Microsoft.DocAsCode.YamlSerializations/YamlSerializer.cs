// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerializations
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.EventEmitters;
    using YamlDotNet.Serialization.NamingConventions;
    using YamlDotNet.Serialization.ObjectGraphVisitors;
    using YamlDotNet.Serialization.TypeInspectors;
    using YamlDotNet.Serialization.TypeResolvers;

    using Microsoft.DocAsCode.YamlSerializations.Helpers;
    using Microsoft.DocAsCode.YamlSerializations.ObjectDescriptors;
    using Microsoft.DocAsCode.YamlSerializations.ObjectGraphTraversalStrategies;
    using Microsoft.DocAsCode.YamlSerializations.TypeInspectors;

    public class YamlSerializer
    {
        internal IList<IYamlTypeConverter> Converters { get; set; }
        private readonly SerializationOptions _options;
        private readonly INamingConvention _namingConvention;
        private readonly ITypeResolver _typeResolver;

        public YamlSerializer(SerializationOptions options = SerializationOptions.None, INamingConvention namingConvention = null)
        {
            _options = options;
            _namingConvention = namingConvention ?? new NullNamingConvention();

            Converters = new List<IYamlTypeConverter>();
            foreach (IYamlTypeConverter yamlTypeConverter in YamlTypeConverters.BuiltInConverters)
            {
                Converters.Add(yamlTypeConverter);
            }

            _typeResolver = IsOptionSet(SerializationOptions.DefaultToStaticType)
                ? (ITypeResolver)new StaticTypeResolver()
                : (ITypeResolver)new DynamicTypeResolver();
        }

        private bool IsOptionSet(SerializationOptions option)
        {
            return (_options & option) != 0;
        }

        public void Serialize(TextWriter writer, object graph)
        {
            Serialize(new Emitter(writer), graph);
        }

        public void Serialize(IEmitter emitter, object graph)
        {
            if (emitter == null)
            {
                throw new ArgumentNullException("emitter");
            }

            EmitDocument(emitter, new BetterObjectDescriptor(graph, graph != null ? graph.GetType() : typeof(object), typeof(object)));
        }

        private void EmitDocument(IEmitter emitter, IObjectDescriptor graph)
        {
            var traversalStrategy = CreateTraversalStrategy();
            var eventEmitter = CreateEventEmitter(emitter);
            var emittingVisitor = CreateEmittingVisitor(emitter, traversalStrategy, eventEmitter, graph);

            emitter.Emit(new StreamStart());
            emitter.Emit(new DocumentStart());

            traversalStrategy.Traverse(graph, emittingVisitor);

            emitter.Emit(new DocumentEnd(true));
            emitter.Emit(new StreamEnd());
        }

        private IObjectGraphVisitor CreateEmittingVisitor(IEmitter emitter, IObjectGraphTraversalStrategy traversalStrategy, IEventEmitter eventEmitter, IObjectDescriptor graph)
        {
            IObjectGraphVisitor emittingVisitor = new EmittingObjectGraphVisitor(eventEmitter);

            emittingVisitor = new CustomSerializationObjectGraphVisitor(emitter, emittingVisitor, Converters);

            if (!IsOptionSet(SerializationOptions.DisableAliases))
            {
                var anchorAssigner = new AnchorAssigner();
                traversalStrategy.Traverse(graph, anchorAssigner);

                emittingVisitor = new AnchorAssigningObjectGraphVisitor(emittingVisitor, eventEmitter, anchorAssigner);
            }

            if (!IsOptionSet(SerializationOptions.EmitDefaults))
            {
                emittingVisitor = new DefaultExclusiveObjectGraphVisitor(emittingVisitor);
            }

            return emittingVisitor;
        }

        private IEventEmitter CreateEventEmitter(IEmitter emitter)
        {
            var writer = new WriterEventEmitter(emitter);

            if (IsOptionSet(SerializationOptions.JsonCompatible))
            {
                return new JsonEventEmitter(writer);
            }
            else
            {
                return new TypeAssigningEventEmitter(writer, IsOptionSet(SerializationOptions.Roundtrip));
            }
        }

        private IObjectGraphTraversalStrategy CreateTraversalStrategy()
        {
            ITypeInspector typeDescriptor = new EmitTypeInspector(_typeResolver);
            if (IsOptionSet(SerializationOptions.Roundtrip))
            {
                typeDescriptor = new ReadableAndWritablePropertiesTypeInspector(typeDescriptor);
            }

            typeDescriptor = new NamingConventionTypeInspector(typeDescriptor, _namingConvention);
            typeDescriptor = new YamlAttributesTypeInspector(typeDescriptor);

            if (IsOptionSet(SerializationOptions.Roundtrip))
            {
                return new RoundtripObjectGraphTraversalStrategy(this, typeDescriptor, _typeResolver, 50);
            }
            else
            {
                return new FullObjectGraphTraversalStrategy(this, typeDescriptor, _typeResolver, 50, _namingConvention);
            }
        }
    }
}
