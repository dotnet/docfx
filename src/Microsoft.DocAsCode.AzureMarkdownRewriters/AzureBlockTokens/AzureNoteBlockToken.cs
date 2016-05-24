// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using MarkdownLite;

    public class AzureNoteBlockToken : MarkdownTextToken
    {

        public AzureNoteBlockToken(IMarkdownRule rule, IMarkdownContext context, string noteType, string content, SourceInfo sourceInfo)
            : base(rule, context, content, sourceInfo)
        {
            NoteType = noteType;
        }

        public string NoteType { get; }
    }
}
