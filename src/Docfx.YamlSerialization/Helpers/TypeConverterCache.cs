// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.Helpers;

/// <summary>
/// A cache / map for <see cref="IYamlTypeConverter"/> instances.
/// </summary>
/// <remarks>
/// This class is copied from following YamlDotNet implementation.
/// https://github.com/aaubry/YamlDotNet/blob/master/YamlDotNet/Serialization/Utilities/TypeConverterCache.cs
/// </remarks>
internal sealed class TypeConverterCache
{
    private readonly IYamlTypeConverter[] typeConverters;
    private readonly ConcurrentDictionary<Type, (bool HasMatch, IYamlTypeConverter? TypeConverter)> cache = new();

    public TypeConverterCache(IEnumerable<IYamlTypeConverter>? typeConverters)
        : this(typeConverters?.ToArray() ?? [])
    {
    }

    public TypeConverterCache(IYamlTypeConverter[] typeConverters)
    {
        this.typeConverters = typeConverters;
    }

    /// <summary>
    /// Returns the first <see cref="IYamlTypeConverter"/> that accepts the given type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to lookup.</param>
    /// <param name="typeConverter">The <see cref="IYamlTypeConverter" /> that accepts this type or <see langword="false" /> if no converter is found.</param>
    /// <returns><see langword="true"/> if a type converter was found; <see langword="false"/> otherwise.</returns>
    public bool TryGetConverterForType(Type type, [NotNullWhen(true)] out IYamlTypeConverter? typeConverter)
    {
        var result = cache.GetOrAdd(type, static (t, tc) => LookupTypeConverter(t, tc), typeConverters);

        typeConverter = result.TypeConverter;
        return result.HasMatch;
    }

    /// <summary>
    /// Returns the <see cref="IYamlTypeConverter"/> of the given type.
    /// </summary>
    /// <param name="converter">The type of the converter.</param>
    /// <returns>The <see cref="IYamlTypeConverter"/> of the given type.</returns>
    /// <exception cref="ArgumentException">If no type converter of the given type is found.</exception>
    /// <remarks>
    /// Note that this method searches on the type of the <see cref="IYamlTypeConverter"/> itself. If you want to find a type converter
    /// that accepts a given <see cref="Type"/>, use <see cref="TryGetConverterForType(Type, out IYamlTypeConverter?)"/> instead.
    /// </remarks>
    public IYamlTypeConverter GetConverterByType(Type converter)
    {
        // Intentially avoids LINQ as this is on a hot path
        foreach (var typeConverter in typeConverters)
        {
            if (typeConverter.GetType() == converter)
            {
                return typeConverter;
            }
        }

        throw new ArgumentException($"{nameof(IYamlTypeConverter)} of type {converter.FullName} not found", nameof(converter));
    }

    private static (bool HasMatch, IYamlTypeConverter? TypeConverter) LookupTypeConverter(Type type, IYamlTypeConverter[] typeConverters)
    {
        // Intentially avoids LINQ as this is on a hot path
        foreach (var typeConverter in typeConverters)
        {
            if (typeConverter.Accepts(type))
            {
                return (true, typeConverter);
            }
        }

        return (false, null);
    }
}
