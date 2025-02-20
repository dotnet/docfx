// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.YamlSerialization.Helpers;
using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.ObjectGraphTraversalStrategies;

/// <summary>
/// An implementation of <see cref="IObjectGraphTraversalStrategy"/> that traverses
/// properties that are read/write, collections and dictionaries, while ensuring that
/// the graph can be regenerated from the resulting document.
/// </summary>
public class RoundtripObjectGraphTraversalStrategy : FullObjectGraphTraversalStrategy
{
    private readonly TypeConverterCache _converters;
    private readonly Settings _settings;

    public RoundtripObjectGraphTraversalStrategy(IEnumerable<IYamlTypeConverter> converters, YamlSerializer serializer, ITypeInspector typeDescriptor, ITypeResolver typeResolver, int maxRecursion, INamingConvention namingConvention, Settings settings, IObjectFactory objectFactory)
        : base(serializer, typeDescriptor, typeResolver, maxRecursion, namingConvention, objectFactory)
    {
        _converters = new TypeConverterCache(converters);
        _settings = settings;
    }

    ////protected override void TraverseProperties<TContext>(IObjectDescriptor value, IObjectGraphVisitor visitor, int currentDepth, IObjectGraphVisitorContext context)
    ////{
    ////    if (!value.Type.HasDefaultConstructor() && !Serializer.Converters.Any(c => c.Accepts(value.Type)))
    ////    {
    ////        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' cannot be deserialized because it does not have a default constructor or a type converter.", value.Type));
    ////    }

    ////    base.TraverseProperties<TContext>(value, visitor, currentDepth, context);
    ////}

    protected override void TraverseProperties<TContext>(IObjectDescriptor value, IObjectGraphVisitor<TContext> visitor, TContext context, Stack<ObjectPathSegment> path, ObjectSerializer serializer)
    {
        if (!value.Type.HasDefaultConstructor(_settings.AllowPrivateConstructors) && !_converters.TryGetConverterForType(value.Type, out _))
        {
            throw new InvalidOperationException($"Type '{value.Type}' cannot be deserialized because it does not have a default constructor or a type converter.");
        }

        base.TraverseProperties(value, visitor, context, path, serializer);
    }
}
