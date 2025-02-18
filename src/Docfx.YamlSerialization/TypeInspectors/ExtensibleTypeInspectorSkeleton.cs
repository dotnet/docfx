// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

using YamlDotNet.Serialization;

namespace Docfx.YamlSerialization.TypeInspectors;

public abstract class ExtensibleTypeInspectorSkeleton : ITypeInspector, IExtensibleTypeInspector
{
    public abstract IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container);

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched, bool caseInsensitivePropertyMatching)
    {
        var candidates = caseInsensitivePropertyMatching
            ? GetProperties(type, container).Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            : GetProperties(type, container).Where(p => p.Name == name);

        using var enumerator = candidates.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            var prop = GetProperty(type, container, name);
            if (prop != null)
            {
                return prop;
            }

            if (ignoreUnmatched)
            {
                return null!;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Property '{0}' not found on type '{1}'.",
                    name,
                    type.FullName
                )
            );
        }

        var property = enumerator.Current;

        if (enumerator.MoveNext())
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Multiple properties with the name/alias '{0}' already exists on type '{1}', maybe you're misusing YamlAlias or maybe you are using the wrong naming convention? The matching properties are: {2}",
                    name,
                    type.FullName,
                    string.Join(", ", candidates.Select(p => p.Name).ToArray())
                )
            );
        }

        return property;
    }

    public virtual IPropertyDescriptor? GetProperty(Type type, object? container, string name) => null;

    public abstract string GetEnumName(Type enumType, string name);

    public abstract string GetEnumValue(object enumValue);
}
