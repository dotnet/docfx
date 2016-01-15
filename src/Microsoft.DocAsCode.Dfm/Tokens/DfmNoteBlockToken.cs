// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmNoteBlockToken : IMarkdownToken
    {
        public DfmNoteBlockToken(IMarkdownRule rule, IMarkdownContext context, string noteType, string content, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
            NoteType = noteType;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Content { get; }

        public string RawMarkdown { get; set; }

        public string NoteType { get; }
    }
}
