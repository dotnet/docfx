// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;

    public class InclusionInlineParser : InlineParser
    {
        private const string StartString = "[!include";

        public InclusionInlineParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
            {
                return false;
            }
            else
            {
                if(slice.CurrentChar == '-')
                {
                    slice.NextChar();
                }
            }

            var includeFile = new InclusionInline();
            string title = null, path = null;

            if (!ExtensionsHelper.MatchLink(ref slice, ref title, ref path))
            {
                return false;
            }

            includeFile.Title = title;
            includeFile.IncludedFilePath = path;
            processor.Inline = includeFile;

            return true;
        }
    }
}
