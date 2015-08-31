// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public abstract class BlockRules
    {

        public virtual Regex Newline { get { return Regexes.Block.Newline; } }

        public virtual Regex Сode { get { return Regexes.Block.Code; } }

        public virtual Regex Fences { get { return Regexes.Noop; } }

        public virtual Regex Hr { get { return Regexes.Block.Hr; } }

        public virtual Regex Heading { get { return Regexes.Block.Heading; } }

        public virtual Regex NpTable { get { return Regexes.Noop; } }

        public virtual Regex LHeading { get { return Regexes.Block.LHeading; } }

        public virtual Regex Blockquote { get { return Regexes.Block.Blockquote; } }

        public virtual Regex List { get { return Regexes.Block.List; } }

        public virtual Regex Html { get { return Regexes.Block.Html; } }

        public virtual Regex Def { get { return Regexes.Block.Def; } }

        public virtual Regex Table { get { return Regexes.Noop; } }

        public virtual Regex Paragraph { get { return Regexes.Block.Paragraph; } }

        public virtual Regex Text { get { return Regexes.Block.Text; } }

        public virtual Regex Bullet { get { return Regexes.Block.Bullet; } }

        public virtual Regex Item { get { return Regexes.Block.Item; } }

    }
}
