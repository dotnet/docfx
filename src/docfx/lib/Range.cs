// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal readonly struct Range
    {
        public readonly int StartLine;
        public readonly int StartCharacter;
        public readonly int EndLine;
        public readonly int EndCharacter;

        public Range(int line, int column)
        {
            StartLine = line;
            StartCharacter = column;
            EndLine = line;
            EndCharacter = column;
        }

        public Range(int startLine, int startCharacter, int endLine, int endCharacter)
        {
            StartLine = startLine;
            StartCharacter = startCharacter;
            EndLine = endLine;
            EndCharacter = endCharacter;
        }
    }
}
