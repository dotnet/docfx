namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class NolocParser : InlineParser
    {
        // syntax => :::no-loc text="{content}":::
        private const string StartString = ":::no-loc text=\"";
        private const string EndString = "\":::";

        public NolocParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            if (!ExtensionsHelper.MatchStart(ref slice, StartString, true))
            {
                return false;
            }

            var text = ExtensionsHelper.TryGetStringBeforeChars(new char[] { '\"', '\n' }, ref slice);

            if(text == null || text.IndexOf('\n') != -1)
            {
                return false;
            }

            if (!ExtensionsHelper.MatchStart(ref slice, EndString, true))
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
