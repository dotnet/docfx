// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal struct Range
    {
        public Range(int startLine, int startCharacter, int endLine = 0, int endCharacter = 0)
        {
            StartLine = startLine;
            StartCharacter = startCharacter;
            EndLine = endLine;
            EndCharacter = endCharacter;
        }

        public int StartLine { get; set; }

        public int StartCharacter { get; set; }

        public int EndLine { get; set; }

        public int EndCharacter { get; set; }

        public override string ToString()
        {
            if (EndLine == 0 && EndCharacter == 0)
            {
                return $"(Line: {StartLine}, Character: {StartCharacter})";
            }
            else
            {
                return $"(Line: {StartLine}, Character: {StartCharacter}) - (Line: {EndLine} Character: {EndCharacter})";
            }
        }
    }
}
