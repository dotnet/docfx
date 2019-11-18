// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.ObjectDescriptors
{
    using System;

    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization.Helpers;

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
}
