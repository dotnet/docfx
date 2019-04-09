// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    [DebuggerDisplay("{Value}")]
    public sealed class SourceInfo<T> : ISourceInfo
    {
        public readonly T Value;

        public readonly string File;

        public readonly Range Range;

        object ISourceInfo.Value => Value;

        public SourceInfo(T value, string file, Range range)
        {
            Value = value;
            File = file;
            Range = range;
        }

        public SourceInfo<T> WithValue(T value)
        {
            if (value == default)
                return null;

            return new SourceInfo<T>(value, File, Range);
        }

        public static implicit operator T(SourceInfo<T> value)
        {
            return value != null ? value.Value : default;
        }
    }
}
