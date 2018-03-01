// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using MarkdownLite;

    public class AzureNoteBlockToken : IMarkdownToken
    {

        public AzureNoteBlockToken(IMarkdownRule rule, IMarkdownContext context, string noteType, string content, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            NoteType = noteType;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Content { get; }

        public string NoteType { get; }

        public SourceInfo SourceInfo { get; }
    }
}
