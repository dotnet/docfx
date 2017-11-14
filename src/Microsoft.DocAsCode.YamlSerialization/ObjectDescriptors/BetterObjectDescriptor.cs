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
            if (scalarStyle == ScalarStyle.Any)
            {
                if (value is string s)
                {
                    if (Regexes.BooleanLike.IsMatch(s))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (Regexes.NullLike.IsMatch(s))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (Regexes.IntegerLike.IsMatch(s))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (Regexes.DoubleLike.IsMatch(s))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (s.StartsWith("'"))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (s.StartsWith("\""))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                    else if (s.Length > 0 && char.IsWhiteSpace(s[0]))
                    {
                        scalarStyle = ScalarStyle.DoubleQuoted;
                    }
                }
            }
            ScalarStyle = scalarStyle;
        }

        public ScalarStyle ScalarStyle { get; }

        public Type StaticType { get; }

        public Type Type { get; }

        public object Value { get; }
    }
}
