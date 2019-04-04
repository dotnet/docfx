// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    [DebuggerDisplay("{Value}")]
    public sealed class SourceInfo<T> : SourceInfo
    {
        public readonly T Value;

        internal override object GetValue() => Value;

        public SourceInfo(T value, string file, int startLine, int startColumn, int endLine, int endColumn)
            : base(file, startLine, startColumn, endLine, endColumn)
        {
            Value = value;
        }

        public static implicit operator T(SourceInfo<T> value)
        {
            return value != null ? value.Value : default;
        }
    }
}
