// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Inline-Level Grammar
    /// </summary>
    public abstract class InlineRules
    {

        public virtual Regex Escape { get { return Regexes.Inline.Escape; } }

        public virtual Regex AutoLink { get { return Regexes.Inline.AutoLink; } }

        public virtual Regex Url { get { return Regexes.Noop; } }

        public virtual Regex Tag { get { return Regexes.Inline.Tag; } }

        public virtual Regex Link { get { return Regexes.Inline.Link; } }

        public virtual Regex RefLink { get { return Regexes.Inline.RefLink; } }

        public virtual Regex NoLink { get { return Regexes.Inline.NoLink; } }

        public virtual Regex Strong { get { return Regexes.Inline.Strong; } }

        public virtual Regex Em { get { return Regexes.Inline.Em; } }

        public virtual Regex Code { get { return Regexes.Inline.Code; } }

        public virtual Regex Br { get { return Regexes.Inline.Br; } }

        public virtual Regex Del { get { return Regexes.Noop; } }

        public virtual Regex Text { get { return Regexes.Inline.Text; } }

    }
}
