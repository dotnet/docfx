// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    [DebuggerDisplay("{Value}")]
    public class SourceInfo<T> : ISourceInfo
    {
        public T Value { get; }

        public string File { get; }

        public Range Range { get; }

        object ISourceInfo.Value => Value;

        public SourceInfo(T value, string file, Range range)
        {
            Value = value;
            File = file;
            Range = range;
        }
    }
}
