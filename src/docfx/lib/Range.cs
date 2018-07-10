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

        public Range(int startLine, int startCharacter, int endLine = 0, int endCharacter = 0)
        {
            StartLine = startLine;
            StartCharacter = startCharacter;
            EndLine = endLine;
            EndCharacter = endCharacter;
        }

        public override string ToString()
        {
            if (EndLine == 0 && EndCharacter == 0)
            {
                return $"(Line: {StartLine}, Character: {StartCharacter})";
            }
            else
            {
                return $"(Line: {StartLine}, Character: {StartCharacter}) - (Line: {EndLine}, Character: {EndCharacter})";
            }
        }
    }
}
