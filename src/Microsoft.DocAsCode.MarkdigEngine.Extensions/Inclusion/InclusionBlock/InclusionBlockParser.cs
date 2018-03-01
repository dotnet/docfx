// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;

    public class InclusionBlockParser : BlockParser
    {
        private const string StartString = "[!include";

        public InclusionBlockParser()
        {
            OpeningCharacters = new char[] { '[' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            // [!include[<title>](<filepath>)]
            var column = processor.Column;
            var line = processor.Line;
            var command = line.ToString();
            var includeFile = new InclusionBlock(this);

            if (!ExtensionsHelper.MatchStart(ref line, StartString, false))
            {
                return BlockState.None;
            }
            else
            {
                if (line.CurrentChar == '+')
                {
                    line.NextChar();
                }
            }

            var context = new InclusionContext();

            if (!ExtensionsHelper.MatchLink(ref line, ref context))
            {
                return BlockState.None;
            }

            while (line.CurrentChar.IsSpaceOrTab()) line.NextChar();
            if (line.CurrentChar != '\0')
            {
                return BlockState.None;
            }

            includeFile.Context = context;
            processor.NewBlocks.Push(includeFile);

            return BlockState.BreakDiscard;
        }
    }
}
