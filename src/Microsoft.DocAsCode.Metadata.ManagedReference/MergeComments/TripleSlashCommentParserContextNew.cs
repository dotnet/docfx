// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.DocAsCode.DataContracts.Common;

    internal sealed class TripleSlashCommentParserContextNew : ITripleSlashCommentParserContext
    {
        public static readonly TripleSlashCommentParserContext Instance = new TripleSlashCommentParserContext
        {
            AddReferenceDelegate = (s, e) => { },
            Normalize = true,
        };

        public Action<string, string> AddReferenceDelegate { get; set; }
        public bool Normalize { get; set; }
        public bool PreserveRawInlineComments { get; set; }
        public SourceDetail Source { get; set; }
    }
}