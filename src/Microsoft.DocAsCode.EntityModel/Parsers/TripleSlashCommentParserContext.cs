// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    public class TripleSlashCommentParserContext : ITripleSlashCommentParserContext
    {
        public bool Normalize { get; set; } = true;

        public bool PreserveRawInlineComments { get; set; }

        public Action<string> AddReferenceDelegate { get; set; }

        public SourceDetail Source { get; set; }
    }
}
