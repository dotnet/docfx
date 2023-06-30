// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.YamlSerialization.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.YamlSerialization.ObjectDescriptors;

public class BetterObjectDescriptor : IObjectDescriptor
{
    public BetterObjectDescriptor(object value, Type type, Type staticType)
        : this(value, type, staticType, ScalarStyle.Any)
    {
    }

    public BetterObjectDescriptor(object value, Type type, Type staticType, ScalarStyle scalarStyle)
    {
        Value = value;
        Type = type;
        StaticType = staticType;
        ScalarStyle = scalarStyle == ScalarStyle.Any && NeedQuote(value) ? ScalarStyle.DoubleQuoted : scalarStyle;

        bool NeedQuote(object val)
        {
            if (!(val is string s))
                return false;

            return Regexes.BooleanLike.IsMatch(s)
                || Regexes.NullLike.IsMatch(s)
                || Regexes.IntegerLike.IsMatch(s)
                || Regexes.FloatLike.IsMatch(s)
                || s.StartsWith("'", StringComparison.Ordinal)
                || s.StartsWith("\"", StringComparison.Ordinal)
                || s.Length > 0 && char.IsWhiteSpace(s[0]);
        }
    }

    public ScalarStyle ScalarStyle { get; }

    public Type StaticType { get; }

    public Type Type { get; }

    public object Value { get; }
}
