// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;

    using Microsoft.DocAsCode.DataContracts.Common;

    public interface ITripleSlashCommentParserContext
    {
        bool PreserveRawInlineComments { get; set; }
        Action<string, string> AddReferenceDelegate { get; set; }
        Func<string, CRefTarget> ResolveCRef { get; }
        SourceDetail Source { get; set; }
        string CodeSourceBasePath { get; set; }
    }
}
