namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class NolocParser : InlineParser
    {
        // syntax => ::: noloc text="{content}" :::
        private const string StartString = "::: noloc text=\"";
        private const string EndString = "\" :::";

        public NolocParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
            {
                return false;
            }

            var text = ExtensionsHelper.TryGetStringBeforeChars(new char[] { '\"', '\n' }, ref slice);

            if(text == null)
            {
                return false;
            }

            if (!ExtensionsHelper.MatchStart(ref slice, EndString, false))
            {
                return false;
            }

            processor.Inline = new NolocInline()
            {
                Text = text
            };

            return true;
        }
    }
}
