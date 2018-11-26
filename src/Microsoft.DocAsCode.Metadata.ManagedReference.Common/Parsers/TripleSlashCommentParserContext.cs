// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.DocAsCode.DataContracts.Common;

    public class TripleSlashCommentParserContext : ITripleSlashCommentParserContext
    {
        public bool PreserveRawInlineComments { get; set; }

        public Action<string, string> AddReferenceDelegate { get; set; }

        public Func<string, CRefTarget> ResolveCRef { get; set; }

        public SourceDetail Source { get; set; }

        public string CodeSourceBasePath { get; set; }
    }
}
